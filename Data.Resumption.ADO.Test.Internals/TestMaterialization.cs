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
            reader.ProcessColumnMap(ColumnMap.Parse(new[] { "X", "Y" }));
            reader.ProcessRow(new object[] { 3, 5 });
            var point = reader.ToEntity();
            Assert.AreEqual(3, point.X);
            Assert.AreEqual(5, point.Y);
        }
    }
}
