using System;
using System.Collections.Generic;

namespace Data.Resumption.Execution
{
    internal class DataSourceCache
    {
        public IComparable DataSource { get; }
        private readonly Dictionary<IComparable, SuccessOrException> _responseByIdentity
            = new Dictionary<IComparable, SuccessOrException>();

        public DataSourceCache(IComparable dataSource)
        {
            DataSource = dataSource;
        }

        public void Clear() => _responseByIdentity.Clear();

        public void Store(IComparable identity, SuccessOrException response) => _responseByIdentity[identity] = response;

        public SuccessOrException? CheckCache(IComparable identity)
        {
            SuccessOrException found;
            if (_responseByIdentity.TryGetValue(identity, out found)) return found;
            return null;
        }
    }
}