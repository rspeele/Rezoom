using System;
using System.Collections.Generic;

namespace Data.Resumption.Execution
{
    internal class ResponseCache
    {
        private readonly DataSourceCache _nullDataSource = new DataSourceCache(null);
        private readonly Dictionary<IComparable, DataSourceCache> _dataSources
            = new Dictionary<IComparable, DataSourceCache>();

        public void Store(IComparable dataSource, IComparable identity, SuccessOrException response)
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

        public void Invalidate(IComparable dataSource)
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

        public SuccessOrException? CheckCache(IComparable dataSource, IComparable identity)
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