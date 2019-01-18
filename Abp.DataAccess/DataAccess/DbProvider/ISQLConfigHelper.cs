using Abp.DataAccess.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Abp.DataAccess.DbProvider
{
    public interface ISQLConfigHelper
    {
        List<SQL> GetSQLList();
    }
}
