using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Text;

namespace Abp.DataAccess
{
    public interface IDbHelper
    {
        void GetConnectionInfo(string connectionKey, out string connectionString);
        int ExecuteNonQuery(string connKey, CommandType cmdType, string cmdText, int timeout, params DbParameter[] commandParameters);

        DbDataReader ExecuteReader(string connKey, CommandType cmdType, string cmdText, int timeout, params DbParameter[] commandParameters);

        object ExecuteScalar(string connKey, CommandType cmdType, string cmdText, int timeout, params DbParameter[] commandParameters);
        DataSet ExecuteDataSet(string connKey, CommandType cmdType, string cmdText, int timeout, params DbParameter[] commandParameters);
        DataTable ExecuteDataTable(string connKey, CommandType cmdType, string cmdText, int timeout, params DbParameter[] commandParameters);
        DataRow ExecuteDataRow(string connKey, CommandType cmdType, string cmdText, int timeout, params DbParameter[] commandParameters);
        string SetSafeParameter(string connKey, string parameterValue);
    }
}
