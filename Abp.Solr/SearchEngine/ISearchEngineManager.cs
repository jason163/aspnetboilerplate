using System;
using System.Collections.Generic;
using System.Text;

namespace Abp.Solr
{
    public interface ISearchEngineManager
    {
        T Query<T>(SearchCondition condition);
    }
}
