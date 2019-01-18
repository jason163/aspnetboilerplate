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

        //private Dictionary<Type, object> s_SearcherDic = new Dictionary<Type, object>();
        private string ServiceBaseUrl;
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
        //public void ParseConfig(XElement config)
        //{
        //    //SolrSearchProvider.ServiceBaseUrl = config.Element("baseUrl").Value.Trim();
        //    XElement xElement = config.Element("searchers");
        //    if (xElement != null)
        //    {
        //        foreach (XElement current in xElement.Descendants("searcher"))
        //        {
        //            System.Type type = System.Type.GetType(current.Attribute("result").Value.Trim(), true);
        //            System.Type type2 = System.Type.GetType(current.Attribute("type").Value.Trim(), true);
        //            if (!this.solrConfigProvider.GetSearcherDic().ContainsKey(type))
        //            {
        //                object value = System.Activator.CreateInstance(type2);
        //                this.solrConfigProvider.GetSearcherDic().Add(type, value);
        //            }
        //        }
        //    }
        //}
    }
}
