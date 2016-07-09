using System;
using System.Collections.Generic;

namespace Data.Resumption.ADO.Materialization
{
    internal class ColumnMap
    {
        private readonly int[] _primMap;
        private readonly ColumnMap[] _navMap;

        public ColumnMap(int[] primMap, ColumnMap[] navMap)
        {
            _primMap = primMap;
            _navMap = navMap;
        }

        public int ColumnIndex(int primId) => _primMap[primId];
        public ColumnMap NavMap(int navId) => _navMap[navId];
    }
}
