using System;
using System.Collections.Generic;
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

        public class User
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public Group[] Groups { get; set; }

            public class Group
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
        }

        [TestMethod]
        public void TestArrayNavProperty()
        {
            var template = RowReaderTemplate<User>.Template;
            var reader = template.CreateReader();
            var columnMap = ColumnMap.Parse(new[] { "Id", "Name", "Groups$Id", "Name" });
            reader.ProcessColumnMap(columnMap);
            reader.ProcessRow(new object[] { 1, "bob", 2, "developers" });
            reader.ProcessRow(new object[] { 1, "bob", 3, "testers" });
            var user = reader.ToEntity();
            Assert.AreEqual(1, user.Id);
            Assert.AreEqual("bob", user.Name);
            Assert.AreEqual(2, user.Groups.Length);
            Assert.AreEqual(2, user.Groups[0].Id);
            Assert.AreEqual("developers", user.Groups[0].Name);
            Assert.AreEqual(3, user.Groups[1].Id);
            Assert.AreEqual("testers", user.Groups[1].Name);
        }

        public class NestUser
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public Group[] Groups { get; set; }

            public class Group
            {
                public int Id { get; set; }
                public string Name { get; set; }
                public Tag[] Tags { get; set; }
            }
            public class Tag
            {
                public int Id { get; set; }
                public string Name { get; set; }
            }
        }

        [TestMethod]
        public void TestNestedArrayNavProperty()
        {
            var template = RowReaderTemplate<NestUser>.Template;
            var reader = template.CreateReader();
            var columnMap = ColumnMap.Parse(new[] { "Id", "Name", "Groups$Id", "Name", "Groups$Tags$Id", "Name" });
            reader.ProcessColumnMap(columnMap);
            reader.ProcessRow(new object[] { 1, "bob", 2, "developers", 4, "t1" });
            reader.ProcessRow(new object[] { 1, "bob", 2, "developers", 5, "t2" });
            reader.ProcessRow(new object[] { 1, "bob", 2, "developers", 6, "t3" });
            reader.ProcessRow(new object[] { 1, "bob", 3, "testers", 4, "t1" });
            reader.ProcessRow(new object[] { 1, "bob", 3, "testers", 5, "t2" });
            reader.ProcessRow(new object[] { 1, "bob", 3, "testers", 7, "t4" });
            var user = reader.ToEntity();
            Assert.AreEqual(1, user.Id);
            Assert.AreEqual("bob", user.Name);
            Assert.AreEqual(2, user.Groups.Length);
            Assert.AreEqual(2, user.Groups[0].Id);
            Assert.AreEqual("developers", user.Groups[0].Name);
            Assert.AreEqual(3, user.Groups[0].Tags.Length);
            Assert.AreEqual("t2", user.Groups[0].Tags[1].Name);
            Assert.AreEqual(3, user.Groups[1].Id);
            Assert.AreEqual("testers", user.Groups[1].Name);
            Assert.AreEqual(3, user.Groups[1].Tags.Length);
            Assert.AreEqual("t4", user.Groups[1].Tags[2].Name);
        }
    }
}
