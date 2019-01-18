using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Abp.Solr
{
    public interface ISearchProvider
    {
        Result Query<Result>(SearchCondition condition);
    }
}
