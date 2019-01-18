using Abp.Modules;
using System;
using System.Collections.Generic;
using System.Text;

namespace Abp.Solr
{
    [DependsOn(typeof(AbpKernelModule))]
    public class AbpSolrModule : AbpModule
    {
        public override void PreInitialize()
        {
            IocManager.Register<ISearchProvider, DataCommand>();
        }

        public override void Initialize()
        {
            base.Initialize();
        }
    }
}
