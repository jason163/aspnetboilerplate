using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using Abp.DataAccess.Configuration;
using Abp.Xml;

namespace Abp.DataAccess.DbProvider
{
    /// <summary>
    /// DB 配置文件提供方
    /// </summary>
    public class DefaultDbConfigProvider : IDbConfigProvider
    {
        public DBConfig ConfigSetting()
        {
            return this.LoadConfig();
        }

        private DBConfig LoadConfig()
        {
            string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"Configuration\Data\DB.config");
            if (File.Exists(filePath))
            {
                DBConfig config = XmlSerializationHelper.LoadFromXml<DBConfig>(filePath);
                return config;
            }
            else
            {
                throw new AbpException(string.Format("Not found sql file {0}", filePath));
            }
        }
    }
}
