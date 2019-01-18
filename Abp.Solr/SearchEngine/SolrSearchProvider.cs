using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Abp.Solr
{
    public class SolrSearchProvider : ISearchProvider
    {
        private ISolrConfigProvider solrConfigProvider;
        
        public SolrSearchProvider(ISolrConfigProvider solrConfig)
        {
            this.solrConfigProvider = solrConfig;
        }

        public Result Query<Result>(SearchCondition condition)
        {
            System.Type typeFromHandle = typeof(Result);
            object obj = null;
            if (!this.solrConfigProvider.GetSearcherDic().TryGetValue(typeFromHandle, out obj) || obj == null)
            {
                throw new System.ApplicationException(string.Format("没有为类型:{0} 配置对应的检索器", typeFromHandle.FullName));
            }
            Searcher<Result> searcher = obj as Searcher<Result>;
            if (searcher == null)
            {
                throw new System.ApplicationException(string.Format("类型:{0} 对应的检索器错误,检索器必须是一个Searcher<>类型", typeFromHandle.FullName));
            }
            return searcher.Query(condition);
        }
        
    }
}
