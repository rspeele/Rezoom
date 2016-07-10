using System;
using Data.Resumption.ADO.Materialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Data.Resumption.ADO.Test.Internals
{
    [TestClass]
    public class TestMaterialization
    {
        public class Point { public int X { get; set; } public int Y { get; set; } }
        [TestMethod]
        public void TestSimplePropertyAssignment()
        {
            var template = RowReaderTemplate<Point>.Template;
            var reader = template.CreateReader();
        }
    }
}
