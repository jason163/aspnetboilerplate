using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;

namespace Abp.DataAccess.Configuration
{
    [Serializable]
    public class DBConfig
    {
        /// <summary>
        /// 数据库连接字符串
        /// </summary>
        public List<DBConnection> DBConnectionList { get; set; }

        /// <summary>
        /// SQL语句的文件列表
        /// </summary>
        [XmlArrayItem("SQLFile")]
        public List<string> SQLFileList { get; set; }
    }
    [Serializable]
    public class DBConnection
    {
        [XmlAttribute]
        public string Key { get; set; }

        [XmlElement]
        public string ConnectionString { get; set; }

        /// <summary>
        /// 数据库类型：SqlServer，Access，MySQL
        /// </summary>
        [XmlAttribute]
        public string DBType { get; set; }

        /// <summary>
        /// 当前的数据提供者对象
        /// </summary>
        [XmlIgnore]
        public ProviderType DBProviderType
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DBType))
                {
                    return ProviderType.OleDb;
                }
                else if (DBType.Trim().ToUpper() == "SQLSERVER")
                {
                    return ProviderType.SqlServer;
                }
                else if (DBType.Trim().ToUpper() == "MYSQL")
                {
                    return ProviderType.MySql;
                }
                else
                {
                    return ProviderType.OleDb;
                }
            }
        }

        /// <summary>
        /// 超时时间
        /// </summary>
        [XmlAttribute]
        public int TimeOut { get; set; }

    }



    public enum ProviderType
    {
        SqlServer,
        MySql,
        OleDb
    }
}
