using System;
using System.Collections.Generic;

namespace Data.Resumption.Execution
{
    internal class ResponseCache
    {
        private readonly DataSourceCache _nullDataSource = new DataSourceCache(null);
        private readonly Dictionary<object, DataSourceCache> _dataSources
            = new Dictionary<object, DataSourceCache>();

        public void Store(object dataSource, object identity, SuccessOrException response)
        {
            if (dataSource == null)
            {
                _nullDataSource.Store(identity, response);
            }
            else
            {
                DataSourceCache dataSourceCache;
                if (!_dataSources.TryGetValue(dataSource, out dataSourceCache))
                {
                    dataSourceCache = new DataSourceCache(dataSource);
                    _dataSources[dataSource] = dataSourceCache;
                }
                dataSourceCache.Store(identity, response);
            }
        }

        public void Invalidate(object dataSource)
        {
            if (dataSource == null)
            {
                _nullDataSource.Clear();
            }
            else
            {
                _dataSources.Remove(dataSource);
            }
        }

        public SuccessOrException? CheckCache(object dataSource, object identity)
        {
            if (dataSource == null)
            {
                return _nullDataSource.CheckCache(identity);
            }
            DataSourceCache dataSourceCache;
            return _dataSources.TryGetValue(dataSource, out dataSourceCache)
                ? dataSourceCache.CheckCache(identity)
                : null;
        }
    }
}