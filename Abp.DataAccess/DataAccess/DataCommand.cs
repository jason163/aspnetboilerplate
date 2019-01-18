using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.OleDb;
using System.Linq;
using System.Reflection;
using System.Collections;
using Abp.DataAccess.Configuration;
using Abp.DataAccess.DbProvider;
using Abp.DataAccess.EntityBasic;
using Abp.FastInvoke;
using Abp.DataMapper;

namespace Abp.DataAccess
{
    public class DataCommand : IDataCommand
    {
        private ISQLConfigHelper ConfigHelper;
        private IDbHelper DBHelper;
        private IDbFactory DBFactory;

        public DataCommand(ISQLConfigHelper configHelper, IDbHelper dbHelper, IDbFactory dbFactory)
        {
            this.ConfigHelper = configHelper;
            this.DBHelper = dbHelper;
            this.DBFactory = dbFactory;
        }

        const string INIT_QUERYCONDITION_STRING = " WHERE 1=1 ";
        private SQL m_SQLNode;
        private string m_ConnString;
        
        private string m_CommandText = string.Empty;

        /// <summary>
        /// 创建一个DataCommand
        /// </summary>
        /// <param name="sqlKey">SQL 语句配置文件中的节点SQLKey，如SQLKey="LoadProduct"</param>
        public DataCommand CreateCommand(string sqlKey)
        {
            if (string.IsNullOrWhiteSpace(sqlKey))
            {
                throw new Exception(string.Format("Can not input an empty sqlKey!", sqlKey));
            }
            m_SQLNode = ConfigHelper.GetSQLList().Find(f => f.SQLKey.Trim().ToUpper() == sqlKey.Trim().ToUpper());
            if (m_SQLNode == null)
            {
                throw new Exception(string.Format("Don't found SQLKey:{0} configuration!", sqlKey));
            }
            DBHelper.GetConnectionInfo(m_SQLNode.ConnectionKey, out m_ConnString);

            m_SQLNode.ParameterList = new List<DbParameter>();
            m_CommandText = m_SQLNode.Text;

            return this;
        }


        /// <summary>
        /// 获取或者设置DataCommand的SQL语句
        /// </summary>
        public string CommandText
        {
            get
            {
                return m_CommandText;
            }
            set
            {
                m_CommandText = value;
            }
        }




        #region 设置参数
        /// <summary>
        /// 手动设置参数
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="dbType"></param>
        /// <param name="value"></param>
        public void SetParameter(string paramName, DbType dbType, object value)
        {
            SetParameter(paramName, dbType, value, 0, ParameterDirection.Input);
        }

        /// <summary>
        /// 手动设置参数
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="dbType"></param>
        /// <param name="value"></param>
        /// <param name="size"></param>
        public void SetParameter(string paramName, DbType dbType, object value, int size)
        {
            SetParameter(paramName, dbType, value, size, ParameterDirection.Input);
        }

        /// <summary>
        /// 手动设置参数
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="dbType"></param>
        /// <param name="value"></param>
        /// <param name="direction"></param>
        public void SetParameter(string paramName, DbType dbType, object value, ParameterDirection direction)
        {
            SetParameter(paramName, dbType, value, 0, direction);
        }

        /// <summary>
        /// 手动设置参数
        /// </summary>
        /// <param name="paramName"></param>
        /// <param name="dbType"></param>
        /// <param name="value"></param>
        /// <param name="size"></param>
        /// <param name="direction"></param>
        public void SetParameter(string paramName, DbType dbType, object value, int size, ParameterDirection direction)
        {
            if (!paramName.StartsWith("@"))
            {
                paramName = "@" + paramName;
            }
            DbParameter p = m_SQLNode.ParameterList.Find(f => f.ParameterName.ToLower() == paramName.ToLower().Trim());
            if (p == null)
            {
                p = DBFactory.CreateParameter();
                p.ParameterName = paramName;
                m_SQLNode.ParameterList.Add(p);
            }
            p.Value = ConvertDbValue(value);
            p.Size = size;
            p.DbType = dbType;
            p.Direction = direction;
        }

        /// <summary>
        /// 直接设置实体的参数，系统将根据类的属性名以及属性数据类型，自动Mapping到SQL中的参数上去
        /// 如果有手动设置参数，则手动设置参数的优先，后面自动匹配的则会跳过已手动设置的参数
        /// </summary>
        /// <typeparam name="T">实体泛型</typeparam>
        /// <param name="entity">实体对象</param>
        /// <param name="manuSetParameter">使用手动设置参数:SetParameter，手动设置参数的优先，后面自动匹配的则会跳过已手动设置的参数</param>
        public void SetParameter<T>(T entity, Action<DataCommand, T> manuSetParameter = null) where T : class
        {
            //手动设置参数的优先，后面自动匹配的则会跳过已手动设置的参数
            if (manuSetParameter != null)
            {
                manuSetParameter(this, entity);
            }

            PropertyInfo[] propArray = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (string propName in m_SQLNode.ParameterNameList)
            {
                if (m_SQLNode.ParameterList.Exists(f => f.ParameterName.ToLower().Trim() == propName.ToLower().Trim()))
                {
                    continue;
                }

                foreach (PropertyInfo prop in propArray)
                {
                    string tempName = "@" + prop.Name.ToLower();
                    if (tempName == propName.ToLower())
                    {
                        DbParameter p = DBFactory.CreateParameter();
                        p.ParameterName = propName;
                        p.DbType = ConvertDbType(prop.PropertyType);
                        p.Direction = ParameterDirection.Input;
                        p.Value = ConvertDbValue(Invoker.PropertyGet(entity, prop.Name));
                        m_SQLNode.ParameterList.Add(p);
                    }
                }

                //TODO:对于对象型属性，可进行多层处理——待定，对性能会有比较大的问题，所以建议多层对象还是手动赋值
                //if(propName.Contains('.'))   {  }
            }
        }


        #endregion 设置参数End

        #region 值类型及赋值转换
        private object ConvertDbValue(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return DBNull.Value;
            }
            //枚举参数值转换为Int
            Type type = value.GetType();
            if (type.IsEnum ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                    && type.GetGenericArguments() != null
                    && type.GetGenericArguments().Length == 1
                    && type.GetGenericArguments()[0].IsEnum))
            {
                return (int)value;
            }
            return value;
        }

        private DbType ConvertDbType(Type type)
        {
            if (type.IsEnum ||
                (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                    && type.GetGenericArguments() != null
                    && type.GetGenericArguments().Length == 1
                    && type.GetGenericArguments()[0].IsEnum))
            {
                return DbType.Int32;
            }
            if ((type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)))
            {
                type = type.GetGenericArguments()[0];

            }
            //TODO:转换所有的
            switch (type.Name.ToLower())
            {
                case "int32":
                    return DbType.Int32;
                case "int16":
                    return DbType.Int16;
                case "int64":
                    return DbType.Int64;
                case "decimal":
                    return DbType.Decimal;
                case "single":
                    return DbType.Decimal;
                case "double":
                    return DbType.Double;
                case "datetime":
                    return DbType.DateTime;
                case "guid":
                    return DbType.Guid;
                case "boolean":
                    return DbType.Int32;
                case "timespan":
                    return DbType.DateTime;
                default:
                    return DbType.String;
            }

        }
        #endregion

        #region DataCommand的CURD方法
        /// <summary>
        /// 标准方法：执行返回DataSet
        /// </summary>
        /// <returns></returns>
        public DataSet ExecuteDataSet()
        {
            DataSet ds = DBHelper.ExecuteDataSet(m_SQLNode.ConnectionKey, CommandType.Text, m_CommandText, m_SQLNode.TimeOut, m_SQLNode.ParameterList.ToArray());
            return ds;
        }

        /// <summary>
        /// 标准方法：执行返回DataTable
        /// </summary>
        /// <returns></returns>
        public DataTable ExecuteDataTable()
        {
            return DBHelper.ExecuteDataTable(m_SQLNode.ConnectionKey, CommandType.Text, m_CommandText, m_SQLNode.TimeOut, m_SQLNode.ParameterList.ToArray());
        }
        public DataTable ExecuteDataTable(EnumColumnList enumColumns)
        {
            DataTable table = ExecuteDataTable();
            ConvertEnumColumn(table, enumColumns);
            return table;
        }

        public DataTable ExecuteDataTable<EnumType>(string enumColumnName) where EnumType : struct
        {
            DataTable table = ExecuteDataTable();
            ConvertEnumColumn<EnumType>(table, enumColumnName);
            return table;
        }


        /// <summary>
        /// 执行返回对象列表
        /// </summary>
        /// <typeparam name="T">实体类型，必须是可无参实例化的class</typeparam>
        /// <param name="manualMapper">可以手动mapping</param>
        /// <returns></returns>
        public List<T> ExecuteEntityList<T>(Action<DataRow, T> manualMapper = null) where T : class, new()
        {
            DataTable dt = ExecuteDataTable();

            List<T> list = new List<T>();
            if (dt != null && dt.Rows.Count > 0)
            {
                list = DataMapperHelper.GetEntityList<T, List<T>>(dt.Rows, true, true, manualMapper);
            }
            return list;
        }
        public List<T> ExecuteEntityList<T>(string dynamicParameters, Action<DataRow, T> manualMapper = null) where T : class, new()
        {
            ParseDynamicParametersForExecute(dynamicParameters);
            return ExecuteEntityList<T>(manualMapper);
        }


        /// <summary>
        /// 标准方法：执行返回第一行的DataRow
        /// </summary>
        /// <returns></returns>
        public DataRow ExecuteDataRow()
        {
            return DBHelper.ExecuteDataRow(m_SQLNode.ConnectionKey, CommandType.Text, m_CommandText, m_SQLNode.TimeOut, m_SQLNode.ParameterList.ToArray());
        }

        /// <summary>
        /// 执行返回一个实体对象，将第一行DataRow转换为实体
        /// </summary>
        /// <typeparam name="T">实体类型，必须是可无参实例化的class</typeparam>
        /// <param name="manualMapper">可以手动mapping</param>
        /// <returns></returns>
        public T ExecuteEntity<T>(Action<DataRow, T> manualMapper = null) where T : class, new()
        {
            DataRow dr = ExecuteDataRow();
            if (dr != null)
            {
                
                T t = DataMapperHelper.GetEntity<T>(dr, true, true, manualMapper);
                return t;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 标准方法：执行返回第一行第一列的值
        /// </summary>
        /// <returns></returns>
        public object ExecuteScalar()
        {
            return DBHelper.ExecuteScalar(m_SQLNode.ConnectionKey, CommandType.Text, m_CommandText, m_SQLNode.TimeOut, m_SQLNode.ParameterList.ToArray());
        }

        /// <summary>
        /// 执行返回第一行第一列的值，并自动转换为对应的类型，如果是泛型值且为空则会返回null
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T ExecuteScalar<T>()
        {
            object v = DataMapperHelper.ConvertIfEnum(ExecuteScalar(), typeof(T));
            return DataConvertor.GetValue<T>(v, null, null);
        }

        /// <summary>
        /// 标准方法：执行无返回值
        /// </summary>
        /// <returns></returns>
        public int ExecuteNonQuery()
        {
            return DBHelper.ExecuteNonQuery(m_SQLNode.ConnectionKey, CommandType.Text, m_CommandText, m_SQLNode.TimeOut, m_SQLNode.ParameterList.ToArray());
        }

        #endregion

        #region 查询专用
        /// <summary>
        /// 查询，返回条件页的DataTable；
        /// 查询的SQL语句有两个，第一个返回总的数量，第二个返回数据结果集；
        /// 查询条件可以使用参数，也可以自己定义拼装，注意拼装时，对参数值都使用SetSafeParameter(string parameterValue)处理一下。
        /// </summary>
        /// <param name="filter">继承QueryFilter的查询条件对象</param>
        /// <returns></returns>
        public QueryResult Query(QueryFilter filter, string defaultSortBy)
        {
            DataSet ds = _queryExecute(filter, defaultSortBy);
            int count = int.Parse(ds.Tables[0].Rows[0][0].ToString());

            QueryResult result = new QueryResult();
            result.data = ds.Tables[1];
            result.PageIndex = filter.PageIndex;
            result.PageSize = filter.PageSize;
            result.SortFields = filter.SortFields;
            result.recordsTotal = count;
            return result;
        }

        /// <summary>
        /// 查询，返回条件页的实体列表
        /// 查询的SQL语句有两个，第一个返回总的数量，第二个返回数据结果集
        /// 查询条件可以使用参数，也可以自己定义拼装，注意拼装时，对参数值都使用SetSafeParameter(string parameterValue)处理一下。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="filter"></param>
        /// <returns></returns>
        public QueryResult<T> Query<T>(QueryFilter filter, string defaultSortBy, Action<DataRow, T> manualMapper = null) where T : class, new()
        {
            DataSet ds = _queryExecute(filter, defaultSortBy);
            int count = int.Parse(ds.Tables[0].Rows[0][0].ToString());

            QueryResult<T> result = new QueryResult<T>();
            result.PageIndex = filter.PageIndex;
            result.PageSize = filter.PageSize;
            result.SortFields = filter.SortFields;
            result.draw = filter.draw;
            result.recordsTotal = count;

            if (ds.Tables[1] != null && ds.Tables[1].Rows != null && ds.Tables[1].Rows.Count > 0)
            {
                result.data = DataMapperHelper.GetEntityList<T, List<T>>(ds.Tables[1].Rows, true, true, manualMapper);
            }
            else
            {
                result.data = new List<T>();
            }

            if (ds.Tables.Count >= 3)
            {
                result.Summary = ds.Tables[2].Rows[0][0] != null ? ds.Tables[2].Rows[0][0].ToString().Trim() : "";
            }

            return result;
        }

        private DataSet _queryExecute(QueryFilter filter, string defaultSortBy)
        {
            if (string.IsNullOrWhiteSpace(defaultSortBy))
            {
                throw new Exception("Default SortBy can not be empty!");
            }
            if (string.IsNullOrWhiteSpace(filter.SortFields))
            {
                filter.SortFields = defaultSortBy;
            }
            m_CommandText = m_CommandText.Replace("@SortFields", filter.SortFields);
            m_CommandText = m_CommandText.Replace("@PageSize", filter.PageSize.ToString());
            m_CommandText = m_CommandText.Replace("@PageIndex", filter.PageIndex.ToString());

            m_CommandText = m_CommandText.Replace("#STRWHERE#", m_QueryConditionString);

            DataSet ds = DBHelper.ExecuteDataSet(m_SQLNode.ConnectionKey, CommandType.Text, m_CommandText, m_SQLNode.TimeOut, m_SQLNode.ParameterList.ToArray());
            if (ds.Tables.Count < 2)
            {
                throw new Exception("Query SQL Script is Error, it should return 2 tables, 1st table is record count, 2rd table is dataresult.");
            }
            int count = 0;
            if (ds.Tables[0].Rows[0][0] == null || !int.TryParse(ds.Tables[0].Rows[0][0].ToString(), out count))
            {
                throw new Exception("Query SQL Script is Error, 1st table is record count, it must be integer.");
            }
            return ds;


        }

        private string m_QueryConditionString = INIT_QUERYCONDITION_STRING;
        public string QueryConditionString
        {
            get
            {
                return m_QueryConditionString;
            }
            set
            {
                m_QueryConditionString = value;
            }
        }

        /// <summary>
        /// 设置查询参数，都是AND模式，如果值为空，将自动不作为条件
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="condOperation"></param>
        /// <param name="parameterDbType"></param>
        /// <param name="objValue"></param>
        public void QuerySetCondition(string fieldName, ConditionOperation condOperation, DbType parameterDbType, object objValue)
        {
            if (objValue == null)
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(objValue.ToString()))
            {
                return;
            }

            string strCond = "";
            switch (condOperation)
            {
                case ConditionOperation.Equal:
                    strCond = string.Format(" AND {0}={1}", fieldName, _buildConditionValue(parameterDbType, objValue));
                    break;
                case ConditionOperation.NotEqual:
                    strCond = string.Format(" AND {0}<>{1}", fieldName, _buildConditionValue(parameterDbType, objValue));
                    break;
                case ConditionOperation.LessThan:
                    strCond = string.Format(" AND {0}<{1}", fieldName, _buildConditionValue(parameterDbType, objValue));
                    break;
                case ConditionOperation.LessThanEqual:
                    strCond = string.Format(" AND {0}<={1}", fieldName, _buildConditionValue(parameterDbType, objValue));
                    break;
                case ConditionOperation.MoreThan:
                    strCond = string.Format(" AND {0}>{1}", fieldName, _buildConditionValue(parameterDbType, objValue));
                    break;
                case ConditionOperation.MoreThanEqual:
                    strCond = string.Format(" AND {0}>={1}", fieldName, _buildConditionValue(parameterDbType, objValue));
                    break;
                case ConditionOperation.Like:
                    strCond = string.Format(" AND {0} LIKE {1}", fieldName, "N'%" + SetSafeParameter(objValue.ToString()) + "%'");
                    break;
                case ConditionOperation.LikeLeft:
                    strCond = string.Format(" AND {0} LIKE {1}", fieldName, "N'" + SetSafeParameter(objValue.ToString()) + "%'");
                    break;
                case ConditionOperation.LikeRight:
                    strCond = string.Format(" AND {0} LIKE {1}", fieldName, "N'%" + SetSafeParameter(objValue.ToString()) + "'");
                    break;
                case ConditionOperation.In:
                    if (typeof(IList).IsAssignableFrom(objValue.GetType()))
                    {
                        if (objValue is List<int> || objValue is List<Int16> || objValue is List<Int32> || objValue is List<Int64>
                            || objValue is List<uint> || objValue is List<UInt16> || objValue is List<UInt32> || objValue is List<UInt64>
                            || objValue is List<decimal> || objValue is List<float> || objValue is List<long> || objValue is List<double>)
                        {
                            string lstStr = string.Empty;
                            foreach (var x in (IEnumerable)objValue)
                            {
                                lstStr += _buildConditionValue(parameterDbType, x) + ",";
                            }
                            if (!string.IsNullOrEmpty(lstStr))
                            {
                                lstStr = lstStr.TrimEnd(',');
                            }
                            if (!string.IsNullOrEmpty(lstStr))
                            {
                                strCond = string.Format(" AND {0} IN ({1})", fieldName, lstStr);
                            }
                        }
                        else
                        {
                            string lstStr = string.Empty;
                            foreach (var x in (IEnumerable)objValue)
                            {
                                lstStr += _buildConditionValue(parameterDbType, x) + ",";
                            }
                            if (!string.IsNullOrEmpty(lstStr))
                            {
                                lstStr = lstStr.TrimEnd(',');
                            }
                            if (!string.IsNullOrEmpty(lstStr))
                            {
                                strCond = string.Format(" AND {0} IN ({1})", fieldName, lstStr);
                            }
                        }
                    }
                    else
                    {
                        strCond = string.Format(" AND {0} IN ({1})", fieldName, objValue.ToString());
                    }
                    break;
                case ConditionOperation.NotIn:
                    strCond = string.Format(" AND {0} NOT IN ({1})", fieldName, objValue.ToString());
                    break;
                default:
                    strCond = string.Format(" AND {0}={1}", fieldName, _buildConditionValue(parameterDbType, objValue));
                    break;
            }
            m_QueryConditionString += strCond;


        }

        private void ParseDynamicParametersForExecute(string dynamicParameters)
        {
            if (m_QueryConditionString.Contains(INIT_QUERYCONDITION_STRING))
            {
                m_QueryConditionString = m_QueryConditionString.Replace(INIT_QUERYCONDITION_STRING, "");
            }
            m_CommandText = m_CommandText.Replace(dynamicParameters, m_QueryConditionString);
        }

        /// <summary>
        /// 设置自定义查询参数，模式由查询条件指定
        /// </summary>
        /// <param name="customerQueryCondition"></param>
        public void QuerySetCondition(string customerQueryCondition)
        {
            if (string.IsNullOrWhiteSpace(customerQueryCondition))
            {
                return;
            }
            m_QueryConditionString += " " + customerQueryCondition;
        }

        private string _buildConditionValue(DbType parameterDbType, object objValue)
        {
            if (objValue.GetType().IsEnum)
            {
                objValue = (int)objValue;
            }
            switch (parameterDbType)
            {
                case DbType.Int16:
                case DbType.Int32:
                case DbType.Int64:
                case DbType.Single:
                case DbType.Decimal:
                case DbType.Double:
                case DbType.DateTimeOffset:
                case DbType.SByte:
                case DbType.Time:
                case DbType.UInt16:
                case DbType.UInt32:
                case DbType.UInt64:
                case DbType.VarNumeric:
                    return SetSafeParameter(objValue.ToString());
                default:
                    return "N'" + SetSafeParameter(objValue.ToString()) + "'";
            }

        }

        /// <summary>
        /// 设置安全参数值，防注入攻击；尤其是在拼装SQL 查询语句时需要
        /// </summary>
        /// <param name="parameterValue"></param>
        /// <returns></returns>
        public string SetSafeParameter(string parameterValue)
        {
            //m_SQLNode.
            string v = DBHelper.SetSafeParameter(m_SQLNode.ConnectionKey, parameterValue);
            return v;
        }
        #endregion


        #region ConvertEnumColumn

        public void ConvertEnumColumn<EnumType>(DataTable table, string columnName) where EnumType : struct
        {
            ConvertEnumColumn(table, columnName, typeof(EnumType));
        }

        public void ConvertEnumColumn<EnumType>(DataTable table, int columnIndex) where EnumType : struct
        {
            ConvertEnumColumn(table, columnIndex, typeof(EnumType));
        }

        public void ConvertEnumColumn<EnumType>(DataTable table, string columnName, string newnewColumnNameForOriginalValue) where EnumType : struct
        {
            ConvertEnumColumn(table, columnName, typeof(EnumType), newnewColumnNameForOriginalValue);
        }

        public void ConvertEnumColumn<EnumType>(DataTable table, int columnIndex, string newnewColumnNameForOriginalValue) where EnumType : struct
        {
            ConvertEnumColumn(table, columnIndex, typeof(EnumType), newnewColumnNameForOriginalValue);
        }

        public void ConvertEnumColumn(DataTable table, string columnName, Type enumType)
        {
            ConvertEnumColumn(table, new EnumColumnList { { columnName, enumType } });
        }

        public void ConvertEnumColumn(DataTable table, int columnIndex, Type enumType)
        {
            ConvertEnumColumn(table, new EnumColumnList { { columnIndex, enumType } });
        }

        public void ConvertEnumColumn(DataTable table, string columnName, Type enumType, string newnewColumnNameForOriginalValue)
        {
            ConvertEnumColumn(table, new EnumColumnList { { columnName, enumType, newnewColumnNameForOriginalValue } });
        }

        public void ConvertEnumColumn(DataTable table, int columnIndex, Type enumType, string newnewColumnNameForOriginalValue)
        {
            ConvertEnumColumn(table, new EnumColumnList { { columnIndex, enumType, newnewColumnNameForOriginalValue } });
        }

        public void ConvertEnumColumn(DataTable table, EnumColumnList enumColumns)
        {
            ConvertColumn(table, enumColumns);
        }

        public void ConvertColumn(DataTable table, EnumColumnList enumColumns)
        {
            if (table == null || table.Rows == null || table.Rows.Count <= 0)
            {
                return;
            }
            if (enumColumns != null && enumColumns.Count > 0)
            {
                foreach (var entry in enumColumns)
                {
                    Type enumType = entry.EnumType;
                    if (!enumType.IsEnum)
                    {
                        throw new ArgumentException("The type '" + enumType.AssemblyQualifiedName + "' is not enum.", "enumColumns");
                    }
                    string columnName = entry.ColumnIndex.HasValue ? table.Columns[entry.ColumnIndex.Value].ColumnName : entry.ColumnName;
                    int index = table.Columns.IndexOf(columnName);
                    if (index >= 0)
                    {
                        table.Columns[index].ColumnName = (entry.NewColumnNameForOriginalValue != null && entry.NewColumnNameForOriginalValue.Trim().Length > 0) ?
                            entry.NewColumnNameForOriginalValue : (columnName + "_ECCentral_Auto_Removed_820319");
                        table.Columns.Add(columnName, enumType);
                        entry.ColumnIndex = index;
                        entry.NewColumnIndex = table.Columns.Count - 1;
                    }
                }
            }

            foreach (DataRow row in table.Rows)
            {
                ConvertColumnFromRow(row, enumColumns);
            }
        }

        private void ConvertColumnFromRow(DataRow row, EnumColumnList enumColumns)
        {
            if (enumColumns != null && enumColumns.Count > 0)
            {
                foreach (var entry in enumColumns)
                {
                    Type enumType = entry.EnumType;
                    if (!enumType.IsEnum)
                    {
                        throw new ArgumentException("The type '" + enumType.AssemblyQualifiedName + "' is not enum.", "enumColumns");
                    }
                    int columnIndex = entry.ColumnIndex.HasValue ? entry.ColumnIndex.Value : row.Table.Columns.IndexOf(entry.ColumnName + "_ECCentral_Auto_Removed_820319");
                    if (columnIndex < 0)
                    {
                        continue;
                    }
                    if (row[columnIndex] == null || row[columnIndex] == DBNull.Value)
                    {
                        row[entry.NewColumnIndex] = DBNull.Value;
                        continue;
                    }
                    object orignalData = row[columnIndex];
                    if (orignalData == null || orignalData == DBNull.Value || orignalData.ToString().Trim().Length <= 0)
                    {
                        row[entry.NewColumnIndex] = DBNull.Value;
                    }
                    row[entry.NewColumnIndex] = DataMapperHelper.ConvertIfEnum(orignalData, enumType);
                }
            }
        }

        #endregion


    }

    public enum ConditionOperation
    {
        Equal,
        NotEqual,
        MoreThan,
        MoreThanEqual,
        LessThan,
        LessThanEqual,
        Like,
        LikeRight,
        LikeLeft,
        In,
        NotIn
    }


    public class EnumColumn
    {
        internal EnumColumn()
        {

        }

        public string ColumnName
        {
            get;
            set;
        }

        public int? ColumnIndex
        {
            get;
            set;
        }

        public Type EnumType
        {
            get;
            set;
        }

        public string NewColumnNameForOriginalValue
        {
            get;
            set;
        }

        internal int NewColumnIndex
        {
            get;
            set;
        }
    }

    public class EnumColumnList : List<EnumColumn>
    {
        public void Add(string columnName, Type enumType)
        {
            this.Add(new EnumColumn { ColumnName = columnName, EnumType = enumType });
        }

        public void Add(int columnIndex, Type enumType)
        {
            this.Add(new EnumColumn { ColumnIndex = columnIndex, EnumType = enumType });
        }

        public void Add(string columnName, Type enumType, string newColumnNameForOriginalValue)
        {
            this.Add(new EnumColumn { ColumnName = columnName, EnumType = enumType, NewColumnNameForOriginalValue = newColumnNameForOriginalValue });
        }

        public void Add(int columnIndex, Type enumType, string newColumnNameForOriginalValue)
        {
            this.Add(new EnumColumn { ColumnIndex = columnIndex, EnumType = enumType, NewColumnNameForOriginalValue = newColumnNameForOriginalValue });
        }
    }

}
