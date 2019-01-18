using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Abp.Solr
{
    public class SearchCondition
    {
        private string keyWord;
        private PagingInfo pagingInfo;
        private List<SortItem> sortItems;
        private List<FilterBase> filters;
        private Expression filterExpression;
        private bool isGroupQuery;
        public string KeyWord
        {
            get
            {
                return this.keyWord;
            }
            set
            {
                this.keyWord = value;
            }
        }
        public PagingInfo PagingInfo
        {
            get
            {
                return this.pagingInfo;
            }
            set
            {
                this.pagingInfo = value;
            }
        }
        public List<SortItem> SortItems
        {
            get
            {
                return this.sortItems;
            }
            set
            {
                this.sortItems = value;
            }
        }
        public List<FilterBase> Filters
        {
            get
            {
                return this.filters;
            }
            set
            {
                this.filters = value;
            }
        }
        public Expression FilterExpression
        {
            get
            {
                return this.filterExpression;
            }
            set
            {
                this.filterExpression = value;
            }
        }
        public bool IsGroupQuery
        {
            get
            {
                return this.isGroupQuery;
            }
            set
            {
                this.isGroupQuery = value;
            }
        }
    }

    #region Filter
    public class FilterBase
    {
        protected string field;
        public string Field
        {
            get
            {
                return this.field;
            }
            set
            {
                this.field = value;
            }
        }
    }

    public class FieldFilter : FilterBase
    {
        private string _value;
        public string Value
        {
            get
            {
                return this._value;
            }
            set
            {
                this._value = value;
            }
        }
        public FieldFilter()
        {
        }
        public FieldFilter(string field, string value)
        {
            this.field = field;
            this._value = value;
        }
    }

    public class RangeFilter : FilterBase
    {
        private string from;
        private string to;
        private bool inclusive;
        public string From
        {
            get
            {
                return this.from;
            }
            set
            {
                this.from = value;
            }
        }
        public string To
        {
            get
            {
                return this.to;
            }
            set
            {
                this.to = value;
            }
        }
        public bool Inclusive
        {
            get
            {
                return this.inclusive;
            }
            set
            {
                this.inclusive = value;
            }
        }
        public RangeFilter()
        {
        }
        public RangeFilter(string field, string from, string to)
        {
            this.field = field;
            this.from = from;
            this.to = to;
            this.inclusive = true;
        }
        public RangeFilter(string field, string from, string to, bool inclusive)
        {
            this.field = field;
            this.from = from;
            this.to = to;
            this.inclusive = inclusive;
        }
    }
    #endregion

    public class PagingInfo
    {
        private int m_PageNumber;
        private int m_PageSize;
        public int PageIndex
        {
            get
            {
                return (this.PageNumber - 1 > 0) ? (this.PageNumber - 1) : 0;
            }
        }
        public int PageNumber
        {
            get;
            set;
        }
        public int PageSize
        {
            get;
            set;
        }
        public PagingInfo()
            : this(1, 10)
        {
        }
        public PagingInfo(int pageNumber, int pageSize)
        {
            this.m_PageNumber = pageNumber;
            this.m_PageSize = pageSize;
        }
    }

    public class SortItem
    {
        private string sortKey;
        private SortOrderType type;
        public string SortKey
        {
            get
            {
                return this.sortKey;
            }
            set
            {
                this.sortKey = value;
            }
        }
        public SortOrderType SortType
        {
            get
            {
                return this.type;
            }
            set
            {
                this.type = value;
            }
        }
    }

    public enum SortOrderType
    {
        ASC,
        DESC
    }
}
