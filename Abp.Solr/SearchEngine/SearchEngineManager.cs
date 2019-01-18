using System;
using System.Collections.Generic;
using System.Text;

namespace Abp.Solr
{
    public class SearchEngineManager : ISearchEngineManager
    {
        private ISearchProvider searchProvider;        

        public SearchEngineManager(ISearchProvider searchProvider)
        {
            this.searchProvider = searchProvider;
        }

        public T Query<T>(SearchCondition condition)
        {
            T result;            
            if (searchProvider != null)
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
