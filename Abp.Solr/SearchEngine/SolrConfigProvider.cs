using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Abp.Solr
{
    public class SolrConfigProvider : ISolrConfigProvider
    {
        private const string NODE_NAME = "SolrSearchConfigFolder";
        private const string DEFAULT_FOLDER = "Configuration";
        private const string CONFIG_FILE = "SearchEngine.config";

        // 搜索引擎提供者列表
        private Dictionary<string, ISearchProvider> s_ProviderDic;
        // 搜索目录列表 如：商品搜索；订单搜索 
        private  Dictionary<Type, string> s_ItemDic;
        // 搜索目录对应的 Seacher列表
        private Dictionary<Type, object> s_SearcherDic;

        private  string m_SettingFolderPath;

        public Dictionary<Type, object> GetSearcherDic()
        {
            return this.s_SearcherDic;
        }
        public Dictionary<Type, string> GetItemDic()
        {
            return this.s_ItemDic;
        }

        public Dictionary<string, ISearchProvider> GetProviderDic()
        {
            return this.s_ProviderDic;
        }

        public SolrConfigProvider()
        {
            s_ProviderDic = new Dictionary<string, ISearchProvider>();
            s_ItemDic = new Dictionary<System.Type, string>();
            m_SettingFolderPath = GetBaseFolderPath();
            LoadConfig();
        }

        private string GetBaseFolderPath()
        {
            string text = null;//ConfigurationManager.AppSettings["SolrSearchConfigFolder"];
            string result;
            if (text == null || text.Trim().Length <= 0)
            {
                result = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DEFAULT_FOLDER);
            }
            else
            {
                string pathRoot = System.IO.Path.GetPathRoot(text);
                if (pathRoot == null || pathRoot.Trim().Length <= 0)
                {
                    text = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, text);
                }
                result = text;
            }
            return result;
        }

        private void LoadConfig()
        {
            string path = System.IO.Path.Combine(m_SettingFolderPath, CONFIG_FILE);
            if (!System.IO.File.Exists(path))
            {
                throw new System.ApplicationException("没有找到SearchEngine配置文件");
            }
            using (System.IO.FileStream fileStream = new System.IO.FileStream(path, System.IO.FileMode.Open, System.IO.FileAccess.Read))
            {
                XDocument xDocument = XDocument.Load(fileStream);
                XElement xElement = xDocument.Root.Element("providers");
                if (xElement != null)
                {
                    IEnumerable<XElement> enumerable = xElement.Descendants("provider");
                    if (enumerable != null && enumerable.Count<XElement>() > 0)
                    {
                        foreach (XElement current in enumerable)
                        {
                            LoadSearchProvider(current);
                            LoadSearcher(current);
                        }
                    }
                }
                XElement xElement2 = xDocument.Root.Element("items");
                if (xElement2 != null)
                {
                    System.Collections.Generic.IEnumerable<XElement> enumerable2 = xElement2.Descendants("item");
                    if (enumerable2 != null && enumerable2.Count<XElement>() > 0)
                    {
                        foreach (XElement current2 in enumerable2)
                        {
                            LoadItem(current2);
                        }
                    }
                }
            }
        }
        
        private void LoadItem(XElement itemCfg)
        {
            string typeName = itemCfg.Attribute("type").Value.Trim();
            string text = itemCfg.Attribute("provider").Value.Trim();
            System.Type type = System.Type.GetType(typeName, true);
            if (s_ItemDic != null && !s_ItemDic.ContainsKey(type))
            {
                s_ItemDic.Add(type, text.ToLower());
            }
        }
        private void LoadSearchProvider(XElement providerCfg)
        {
            ISearchProvider searchProvider = null;
            string text = providerCfg.Attribute("name").Value.Trim();
            string typeName = providerCfg.Attribute("type").Value.Trim();
            if (searchProvider != null && !s_ProviderDic.ContainsKey(text))
            {
                s_ProviderDic.Add(text.ToLower(), searchProvider);
            }
        }

        private void LoadSearcher(XElement config)
        {
            XElement xElement = config.Element("searchers");
            XElement urlEle = config.Element("baseUrl");
            if (xElement != null)
            {
                foreach (XElement current in xElement.Descendants("searcher"))
                {
                    System.Type type = System.Type.GetType(current.Attribute("result").Value.Trim(), true);
                    System.Type type2 = System.Type.GetType(current.Attribute("type").Value.Trim(), true);
                    if (!s_SearcherDic.ContainsKey(type))
                    {
                        object value = System.Activator.CreateInstance(type2,urlEle.Value.Trim());
                        s_SearcherDic.Add(type, value);
                    }
                }
            }
        }
    }
}
