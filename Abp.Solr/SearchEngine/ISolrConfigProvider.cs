using System;
using System.Collections.Generic;
using System.Text;

namespace Abp.Solr
{
    public interface ISolrConfigProvider
    {
        // 搜索引擎提供者列表
        Dictionary<string, ISearchProvider> GetProviderDic();
        // 搜索目录列表 如：商品搜索；订单搜索 
        Dictionary<Type, string> GetItemDic();
        // 搜索目录对应的 Seacher列表
        Dictionary<Type, object> GetSearcherDic();
    }
}
