using CommonServiceLocator;
using SolrNet;
using SolrNet.Commands.Parameters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Abp.Solr
{
    public abstract class Searcher<Result>
    {
        public Searcher()
        {
            this.Init();
        }
        protected abstract Result GetSearchResult(SearchCondition condition);
        protected virtual void Init()
        {
        }
        public Result Query(SearchCondition condition)
        {
            return this.GetSearchResult(condition);
        }
    }

    public abstract class SolrSearcher<Record, Result> : Searcher<Result>
    {
        private static bool s_SolrNetComponentHasInitialized = false;
        protected abstract string SolrCoreName
        {
            get;
        }
        protected override Result GetSearchResult(SearchCondition condition)
        {
            ISolrOperations<Record> solr = null;
            try
            {
                solr = ServiceLocator.Current.GetInstance<ISolrOperations<Record>>();
            }
            catch
            {
                throw new ConfigurationErrorsException(this.SolrCoreName + "，没有进行初始化");
            }
            return this.GetSearchResult(condition, solr);
        }
        protected virtual Result GetSearchResult(SearchCondition condition, ISolrOperations<Record> solr)
        {
            QueryOptions queryOptions = this.BuildQueryOptions(condition);
            List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
            if (condition.KeyWord != "*:*")
            {
                list.Add(new KeyValuePair<string, string>("defType", "lucene"));
            }
            if (queryOptions.ExtraParams == null || queryOptions.ExtraParams.Count<KeyValuePair<string, string>>() > 0)
            {
                List<KeyValuePair<string, string>> oldExtraParams = new List<KeyValuePair<string, string>>(queryOptions.ExtraParams);
                list.RemoveAll((KeyValuePair<string, string> v) => oldExtraParams.Any((KeyValuePair<string, string> f) => f.Key == v.Key));
                list.AddRange(oldExtraParams);
            }
            queryOptions.ExtraParams = list.ToArray();
            ISolrQuery query = new SolrQuery(condition.KeyWord);
            SolrQueryResults<Record> solrQueryResult = solr.Query(query, queryOptions);
            return this.TransformSolrQueryResult(solrQueryResult, condition);
        }
        protected virtual QueryOptions BuildQueryOptions(SearchCondition condition)
        {
            QueryOptions queryOptions = new QueryOptions();
            if (condition.KeyWord != null && condition.KeyWord != "*:*")
            {
                condition.KeyWord = condition.KeyWord.Replace("+", "\\+").Replace("-", "\\-").Replace("\"", "\\\"").Replace("'", "\\'").Replace("*", "\\*").Replace(":", "\\:").Trim();
            }
            if (string.IsNullOrEmpty(condition.KeyWord))
            {
                condition.KeyWord = "*:*";
            }
            if (condition.PagingInfo == null)
            {
                condition.PagingInfo = new PagingInfo
                {
                    PageSize = 24,
                    PageNumber = 1
                };
            }
            queryOptions.Rows = new int?((condition.PagingInfo.PageSize >= 0) ? condition.PagingInfo.PageSize : 24);
            queryOptions.Start = ((condition.PagingInfo.PageNumber > 1) ? (queryOptions.Rows * (condition.PagingInfo.PageNumber - 1)) : new int?(0));
            if (condition.SortItems != null && condition.SortItems.Count > 0)
            {
                List<SortOrder> list = new List<SortOrder>();
                foreach (SortItem current in condition.SortItems)
                {
                    if (current.SortType == SortOrderType.ASC)
                    {
                        SortOrder item = new SortOrder(current.SortKey, Order.ASC);
                        list.Add(item);
                    }
                    else
                    {
                        SortOrder item = new SortOrder(current.SortKey, Order.DESC);
                        list.Add(item);
                    }
                }
                queryOptions.OrderBy = list.ToArray();
            }
            if (condition.Filters != null && condition.Filters.Count > 0)
            {
                foreach (FilterBase current2 in condition.Filters)
                {
                    if (current2 is RangeFilter)
                    {
                        RangeFilter rangeFilter = current2 as RangeFilter;
                        if (!string.IsNullOrWhiteSpace(rangeFilter.From) || !string.IsNullOrWhiteSpace(rangeFilter.To))
                        {
                            queryOptions.FilterQueries.Add(new SolrQueryByRange<string>(rangeFilter.Field, rangeFilter.From, rangeFilter.To, rangeFilter.Inclusive, rangeFilter.Inclusive));
                        }
                    }
                    else
                    {
                        if (current2 is FieldFilter)
                        {
                            FieldFilter fieldFilter = current2 as FieldFilter;
                            if (!string.IsNullOrWhiteSpace(fieldFilter.Value))
                            {
                                queryOptions.FilterQueries.Add(new SolrQueryByField(fieldFilter.Field, fieldFilter.Value));
                            }
                        }
                    }
                }
            }
            AbstractSolrQuery abstractSolrQuery = SolrSearcher<Record, Result>.ComputerExpression(condition.FilterExpression);
            if (abstractSolrQuery != null)
            {
                queryOptions.FilterQueries.Add(abstractSolrQuery);
            }
            return queryOptions;
        }
        protected abstract Result TransformSolrQueryResult(SolrQueryResults<Record> solrQueryResult, SearchCondition condition);
        private static AbstractSolrQuery ComputerExpression(Expression expression)
        {
            AbstractSolrQuery result;
            if (expression == null)
            {
                result = null;
            }
            else
            {
                if (!expression.HasChild())
                {
                    result = SolrSearcher<Record, Result>.Compute(expression.NodeData, expression.Operation);
                }
                else
                {
                    AbstractSolrQuery a = SolrSearcher<Record, Result>.ComputerExpression(expression.LeftNode);
                    AbstractSolrQuery b = SolrSearcher<Record, Result>.ComputerExpression(expression.RightNode);
                    result = SolrSearcher<Record, Result>.Compute(a, b, expression.Operation);
                }
            }
            return result;
        }
        private static AbstractSolrQuery Compute(AbstractSolrQuery a, AbstractSolrQuery b, Operation op)
        {
            AbstractSolrQuery result;
            switch (op)
            {
                case Operation.AND:
                    if (a != null && b != null)
                    {
                        result = (a && b);
                    }
                    else
                    {
                        if (a != null)
                        {
                            result = a;
                        }
                        else
                        {
                            result = b;
                        }
                    }
                    break;
                case Operation.OR:
                    if (a != null && b != null)
                    {
                        result = (a || b);
                    }
                    else
                    {
                        if (a != null)
                        {
                            result = a;
                        }
                        else
                        {
                            result = b;
                        }
                    }
                    break;
                case Operation.NOT:
                    if (a != null)
                    {
                        result = !a;
                    }
                    else
                    {
                        if (b != null)
                        {
                            result = !b;
                        }
                        else
                        {
                            result = null;
                        }
                    }
                    break;
                default:
                    if (a != null)
                    {
                        result = a;
                    }
                    else
                    {
                        if (b != null)
                        {
                            result = b;
                        }
                        else
                        {
                            result = null;
                        }
                    }
                    break;
            }
            return result;
        }
        private static AbstractSolrQuery Compute(FilterBase filter, Operation op)
        {
            AbstractSolrQuery result;
            if (filter != null)
            {
                if (filter is RangeFilter)
                {
                    RangeFilter rangeFilter = filter as RangeFilter;
                    result = SolrSearcher<Record, Result>.Compute(new SolrQueryByRange<string>(rangeFilter.Field, rangeFilter.From, rangeFilter.To, rangeFilter.Inclusive), null, op);
                    return result;
                }
                if (filter is FieldFilter)
                {
                    FieldFilter fieldFilter = filter as FieldFilter;
                    result = SolrSearcher<Record, Result>.Compute(new SolrQueryByField(fieldFilter.Field, fieldFilter.Value), null, op);
                    return result;
                }
            }
            result = null;
            return result;
        }
        protected override void Init()
        {
            base.Init();
            
        }
        public SolrSearcher(string serviceUrl)
        {
            this.Init();
            if (!SolrSearcher<Record, Result>.s_SolrNetComponentHasInitialized)
            {
                string serverURL = Path.Combine(serviceUrl, this.SolrCoreName);
                Startup.Init<Record>(serverURL);
                SolrSearcher<Record, Result>.s_SolrNetComponentHasInitialized = true;
            }
        }
    }

    public class ConfigurationErrorsException : Exception
    {
        public ConfigurationErrorsException(string message)
            : base(message)
        {
        }
        public ConfigurationErrorsException(string message, System.Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
