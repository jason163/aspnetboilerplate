using Abp.Modules;
using Abp.Reflection.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using Abp.DataAccess.DbProvider;

namespace Abp.DataAccess
{
    [DependsOn(typeof(AbpKernelModule))]
    public class AbpDataAccessModule : AbpModule
    {
        public override void PreInitialize()
        {
            // 注册SQL Server
            IocManager.Register<IDbFactory, SqlServerFactory>();
            IocManager.Register<IDbConfigProvider, DefaultDbConfigProvider>();
            IocManager.Register<ISQLConfigHelper, SQLConfigHelper>();
            IocManager.Register<IDbHelper, DbHelper>();
            IocManager.Register<IDataCommand, DataCommand>();
        }

        public override void Initialize()
        {
            // 注册当前程序集
            IocManager.RegisterAssemblyByConvention(typeof(AbpDataAccessModule).GetAssembly());
        }

    }
}
