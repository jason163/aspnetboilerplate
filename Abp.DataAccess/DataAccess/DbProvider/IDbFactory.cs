using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Abp.DataAccess.DbProvider
{
    /// <summary>
    /// Db工厂
    /// </summary>
    public interface IDbFactory
    {
        DbCommand CreateCommand();
        DbConnection CreateConnection();
        DbConnection CreateConnection(string connectionString);
        DbDataAdapter CreateDataAdapter();
        DbParameter CreateParameter();

        string SetSafeParameter(string parameterValue);
    }
}
