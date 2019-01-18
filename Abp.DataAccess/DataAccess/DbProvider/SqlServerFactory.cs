using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;

namespace Abp.DataAccess.DbProvider
{
    /// <summary>
    /// SQL SERVER
    /// </summary>
    public class SqlServerFactory : IDbFactory
    {
        public SqlServerFactory()
        {

        }

        public DbCommand CreateCommand()
        {
            return new SqlCommand();
        }

        public DbConnection CreateConnection()
        {
            return new SqlConnection();
        }

        public DbConnection CreateConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }

        public DbDataAdapter CreateDataAdapter()
        {
            return new SqlDataAdapter();
        }

        public DbParameter CreateParameter()
        {
            return new SqlParameter();
        }

        public string SetSafeParameter(string parameterValue)
        {
            string v = parameterValue.Replace("'", "''");
            return v;
        }
    }
}
