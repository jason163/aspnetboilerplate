using System;
using System.Collections.Generic;
using System.Text;
using Abp.DataAccess.Configuration;

namespace Abp.DataAccess.DbProvider
{
    public interface IDbConfigProvider
    {
        /// <summary>
        /// 加载配置文件
        /// </summary>
        /// <returns></returns>
        DBConfig ConfigSetting();
    }
}
