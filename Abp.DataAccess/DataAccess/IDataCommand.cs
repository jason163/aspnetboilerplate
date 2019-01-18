using Abp.DataAccess.EntityBasic;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;

namespace Abp.DataAccess
{
    public interface IDataCommand
    {
        DataCommand CreateCommand(string sqlKey);
        void SetParameter(string paramName, DbType dbType, object value);
        void SetParameter(string paramName, DbType dbType, object value, int size);
        void SetParameter(string paramName, DbType dbType, object value, ParameterDirection direction);
        void SetParameter(string paramName, DbType dbType, object value, int size, ParameterDirection direction);
        void SetParameter<T>(T entity, Action<DataCommand, T> manuSetParameter = null) where T : class;


        DataSet ExecuteDataSet();
        DataTable ExecuteDataTable();
        DataTable ExecuteDataTable(EnumColumnList enumColumns);
        DataTable ExecuteDataTable<EnumType>(string enumColumnName) where EnumType : struct;
        List<T> ExecuteEntityList<T>(Action<DataRow, T> manualMapper = null) where T : class, new();
        List<T> ExecuteEntityList<T>(string dynamicParameters, Action<DataRow, T> manualMapper = null) where T : class, new();
        DataRow ExecuteDataRow();
        T ExecuteEntity<T>(Action<DataRow, T> manualMapper = null) where T : class, new();
        object ExecuteScalar();
        T ExecuteScalar<T>();
        int ExecuteNonQuery();
        QueryResult Query(QueryFilter filter, string defaultSortBy);
        QueryResult<T> Query<T>(QueryFilter filter, string defaultSortBy, Action<DataRow, T> manualMapper = null) where T : class, new();
        void QuerySetCondition(string fieldName, ConditionOperation condOperation, DbType parameterDbType, object objValue);
        void QuerySetCondition(string customerQueryCondition);
        string SetSafeParameter(string parameterValue);

    }
}
