using System;
using System.Collections.Generic;
using System.Text;

namespace Abp.Solr
{
    public class SearchEngineManager : ISearchEngineManager
    {
        private ISolrConfigProvider configProvider;

        public SearchEngineManager(ISolrConfigProvider configProvider)
        {
            this.configProvider = configProvider;
        }

        public T Query<T>(SearchCondition condition)
        {
            T result;
            ISearchProvider searchProvider=null;
            
            if (this.configProvider.TryGetProvider(typeof(T), out searchProvider) && searchProvider != null)
            {
                result = searchProvider.Query<T>(condition);
            }
            else
            {
                result = default(T);
            }
            return result;
        }
    }
}
