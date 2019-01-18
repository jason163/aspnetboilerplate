using System;
using System.Configuration;
using System.Data;
using System.Text;
using System.Data.SqlClient;
using System.Data.Common;
using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Linq;
using Abp.DataAccess.Configuration;
using Abp.DataAccess.DbProvider;
using Abp.Transactions;

namespace Abp.DataAccess
{
    public class DbHelper : IDbHelper
    {
        private readonly IDbConfigProvider _configProvider;
        private readonly IDbFactory _dbFactory;

        public DbHelper(IDbConfigProvider configProvider,IDbFactory dbFactory)
        {
            this._configProvider = configProvider;
            this._dbFactory = dbFactory;
        }

        public void GetConnectionInfo(string connectionKey, out string connectionString)
        {
            DBConnection conn = _configProvider.ConfigSetting().DBConnectionList.Find(f => f.Key.ToUpper().Trim() == connectionKey.ToUpper().Trim());
            if (conn == null)
            {
                throw new Exception(string.Format("Don't found DBConnection Key", connectionKey));
            }
            connectionString = conn.ConnectionString;
        }

        private ConnectionWrapper<DbConnection> GetOpenConnection(string connectionString, IDbFactory factory)
        {
            return GetOpenConnection(connectionString, factory, true);
        }

        private ConnectionWrapper<DbConnection> GetOpenConnection(string connectionString, IDbFactory factory,
            bool disposeInnerConnection)
        {
            return TransactionScopeConnections.GetOpenConnection(connectionString, () => factory.CreateConnection(), disposeInnerConnection);
        }

        public int ExecuteNonQuery(string connKey, CommandType cmdType, string cmdText, int timeout, params DbParameter[] commandParameters)
        {
            string connectionString;
            GetConnectionInfo(connKey, out connectionString);
            DbCommand cmd = _dbFactory.CreateCommand();
            ConnectionWrapper<DbConnection> wrapper = null;
            try
            {
                wrapper = GetOpenConnection(connectionString, _dbFactory);
                PrepareCommand(cmd, wrapper.Connection, null, cmdType, cmdText, timeout, commandParameters);
                int val = cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();
                return val;
            }
            catch (Exception ex)
            {
                throw new DataAccessException(ex, connectionString, cmdText, commandParameters);
            }
            finally
            {
                if (wrapper != null)
                {
                    wrapper.Dispose();
                }
            }
        }

        public DbDataReader ExecuteReader(string connKey, CommandType cmdType, string cmdText, int timeout, params DbParameter[] commandParameters)
        {
            string connectionString;
            GetConnectionInfo(connKey, out connectionString);
            DbCommand cmd = _dbFactory.CreateCommand();

            CommandBehavior cmdBehavior;
            if (Transaction.Current != null)
            {
                cmdBehavior = CommandBehavior.Default;
            }
            else
            {
                cmdBehavior = CommandBehavior.CloseConnection;
            }

            ConnectionWrapper<DbConnection> wrapper = null;
            try
            {
                wrapper = GetOpenConnection(connectionString, _dbFactory);
                PrepareCommand(cmd, wrapper.Connection, null, cmdType, cmdText, timeout, commandParameters);
                DbDataReader rdr = cmd.ExecuteReader(cmdBehavior);
                // cmd.Parameters.Clear();
                return rdr;
            }
            catch (Exception ex)
            {
                if (wrapper != null)
                {
                    wrapper.Dispose();
                }
                throw new DataAccessException(ex, connectionString, cmdText, commandParameters);
            }
        }

        public object ExecuteScalar(string connKey, CommandType cmdType, string cmdText, int timeout, params DbParameter[] commandParameters)
        {
            string connectionString;
            GetConnectionInfo(connKey, out connectionString);
            DbCommand cmd = _dbFactory.CreateCommand();

            ConnectionWrapper<DbConnection> wrapper = null;
            try
            {
                wrapper = GetOpenConnection(connectionString, _dbFactory);
                PrepareCommand(cmd, wrapper.Connection, null, cmdType, cmdText, timeout, commandParameters);
                object val = cmd.ExecuteScalar();
                cmd.Parameters.Clear();
                return val;
            }
            catch (Exception ex)
            {
                throw new DataAccessException(ex, connectionString, cmdText, commandParameters);
            }
            finally
            {
                if (wrapper != null)
                {
                    wrapper.Dispose();
                }
            }
        }

        public DataSet ExecuteDataSet(string connKey, CommandType cmdType, string cmdText, int timeout, params DbParameter[] commandParameters)
        {
            string connectionString;
            GetConnectionInfo(connKey, out connectionString);

            DbCommand cmd = _dbFactory.CreateCommand();
            DataSet ds = new DataSet();
            ConnectionWrapper<DbConnection> wrapper = null;
            try
            {
                wrapper = GetOpenConnection(connectionString, _dbFactory);
                PrepareCommand(cmd, wrapper.Connection, null, cmdType, cmdText, timeout, commandParameters);
                DbDataAdapter sda = _dbFactory.CreateDataAdapter();
                sda.SelectCommand = cmd;
                sda.Fill(ds);
                cmd.Parameters.Clear();
            }
            catch (Exception ex)
            {
                throw new DataAccessException(ex, connectionString, cmdText, commandParameters);
            }
            finally
            {
                if (wrapper != null)
                {
                    wrapper.Dispose();
                }
            }
            return ds;
        }

        public DataTable ExecuteDataTable(string connKey, CommandType cmdType, string cmdText, int timeout, params DbParameter[] commandParameters)
        {
            string connectionString;
            GetConnectionInfo(connKey, out connectionString);

            DbCommand cmd = _dbFactory.CreateCommand();
            DataTable table = new DataTable();
            ConnectionWrapper<DbConnection> wrapper = null;
            try
            {
                wrapper = GetOpenConnection(connectionString, _dbFactory);
                PrepareCommand(cmd, wrapper.Connection, null, cmdType, cmdText, timeout, commandParameters);
                DbDataAdapter sda = _dbFactory.CreateDataAdapter();
                sda.SelectCommand = cmd;
                sda.Fill(table);
                cmd.Parameters.Clear();
            }
            catch (Exception ex)
            {
                throw new DataAccessException(ex, connectionString, cmdText, commandParameters);
            }
            finally
            {
                if (wrapper != null)
                {
                    wrapper.Dispose();
                }
            }
            return table;
        }

        public DataRow ExecuteDataRow(string connKey, CommandType cmdType, string cmdText, int timeout, params DbParameter[] commandParameters)
        {
            DataTable table = ExecuteDataTable(connKey, cmdType, cmdText, timeout, commandParameters);
            if (table.Rows.Count == 0)
            {
                return null;
            }
            return table.Rows[0];
        }

        private void PrepareCommand(DbCommand cmd, DbConnection conn, DbTransaction trans, CommandType cmdType,
            string cmdText, int timeout, DbParameter[] cmdParms)
        {
            if (conn.State != ConnectionState.Open)
            {
                conn.Open();
            }

            cmd.Connection = conn;
            cmd.CommandText = cmdText;
            cmd.CommandTimeout = timeout;

            if (trans != null)
            {
                cmd.Transaction = trans;
            }

            cmd.CommandType = cmdType;

            if (cmdParms != null)
            {
                foreach (DbParameter parm in cmdParms)
                {
                    cmd.Parameters.Add(parm);
                }
            }
        }

        public string SetSafeParameter(string connKey, string parameterValue)
        {
            string connectionString;
            GetConnectionInfo(connKey, out connectionString);
            return _dbFactory.SetSafeParameter(parameterValue);

        }



    }
}
