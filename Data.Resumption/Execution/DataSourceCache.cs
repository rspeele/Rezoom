using System;
using System.Collections.Generic;

namespace Data.Resumption.Execution
{
    internal class DataSourceCache
    {
        public object DataSource { get; }
        private readonly Dictionary<object, SuccessOrException> _responseByIdentity
            = new Dictionary<object, SuccessOrException>();

        public DataSourceCache(object dataSource)
        {
            DataSource = dataSource;
        }

        public void Clear() => _responseByIdentity.Clear();

        public void Store(object identity, SuccessOrException response) => _responseByIdentity[identity] = response;

        public SuccessOrException? CheckCache(object identity)
        {
            SuccessOrException found;
            if (_responseByIdentity.TryGetValue(identity, out found)) return found;
            return null;
        }
    }
}