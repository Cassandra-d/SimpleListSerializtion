using ListSerialization;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace Tests
{
    public class ListTests
    {
        private string GetTempFilepath()
            => Path.GetTempFileName() + Guid.NewGuid().ToString("N");

        private IEnumerable<string> GenerateFiveValues()
        {
            yield return "1";
            yield return "qwerty";
            yield return "/\'\\;_;/'/\"";
            yield return "2 3";
            yield return "Ӂ";

        }

        private ListFacade CreateFileValuesList()
        {
            var lst = new ListFacade();
            foreach (var v in GenerateFiveValues())
                lst.Add(v);

            return lst;
        }

        private bool AreEqual(ListRand expected, ListRand actual)
        {
            if (expected == null && actual != null)
                return false;
            if (expected != null && actual == null)
                return false;

            if (expected.Count != actual.Count)
                return false;

            var expectedDec = new ListFacade(expected);
            var actualDec = new ListFacade(actual);

            bool result = true;
            for (int i = 0; i < expected.Count; i++)
            {
                var expectedNode = expectedDec.ElementAt(i);
                var actualNode = actualDec.ElementAt(i);

                bool sameData = string.Compare(expectedNode.Data, actualNode.Data, System.StringComparison.Ordinal) == 0;
                bool sameRandomRefs = (expectedNode.Rand != null && actualNode.Rand != null) || (expectedNode.Rand == null && actualNode.Rand == null);

                result &= sameData & sameRandomRefs;
            }

            return result;
        }

        [Test]
        public void CreateList_RandomData_Success()
        {
            var lst = new ListFacade();
            foreach (var v in GenerateFiveValues())
                lst.Add(v);
        }

        [Test]
        public void CompareList_SameData_AreEqual()
        {
            var lst = CreateFileValuesList();

            var lst1 = CreateFileValuesList();
            lst1.Lst.Head.Rand = lst1.Lst.Tail;

            var lst2 = CreateFileValuesList();
            lst2.Lst.Head.Rand = lst2.Lst.Tail;


            Assert.True(AreEqual(lst.Lst, lst.Lst));
            Assert.True(AreEqual(lst1.Lst, lst2.Lst));
        }

        [Test]
        public void CompareList_NotSameData_AreNotEqual()
        {
            var lst = CreateFileValuesList();

            var lst1 = CreateFileValuesList();
            lst1.Lst.Tail.Data = "x";

            var lst2 = CreateFileValuesList();
            lst2.Lst.Head.Rand = lst.Lst.Tail;

            Assert.False(AreEqual(lst.Lst, lst1.Lst));
            Assert.False(AreEqual(lst.Lst, lst2.Lst));
        }

        [Test]
        public void DeserializeList_SpecialSymbols_Success()
        {
            ListFacade expected;
            ListRand actual;
            string path = GetTempFilepath();
            expected = CreateFileValuesList();

            using (var fs = new FileStream(path, FileMode.CreateNew))
            {
                expected.Serialize(fs);
                fs.Flush();
            }

            using (var fs = new FileStream(path, FileMode.Open))
            {
                actual = ListRand.Deserialize(fs);
            }

            Assert.True(AreEqual(expected.Lst, actual));
        }

        [Test]
        public void DeserializeList_NullData_Success()
        {
            ListFacade expected;
            ListRand actual;
            string path = GetTempFilepath();
            expected = CreateFileValuesList();

            expected.Lst.Tail.Data = null;

            using (var fs = new FileStream(path, FileMode.CreateNew))
            {
                expected.Serialize(fs);
                fs.Flush();
            }

            using (var fs = new FileStream(path, FileMode.Open))
            {
                actual = ListRand.Deserialize(fs);
            }

            Assert.True(AreEqual(expected.Lst, actual));
        }

        [Test]
        public void DeserializeList_ReferencedNodeAfterReferencing_Success()
        {
            ListFacade expected;
            ListRand actual;
            string path = GetTempFilepath();
            expected = CreateFileValuesList();

            ListNode lastNode = expected.Lst.Tail.Prev;
            while (lastNode.Prev != null)
                lastNode = lastNode.Prev;
            expected.Lst.Head.Rand = lastNode;

            using (var fs = new FileStream(path, FileMode.CreateNew))
            {
                expected.Serialize(fs);
                fs.Flush();
            }

            using (var fs = new FileStream(path, FileMode.Open))
            {
                actual = ListRand.Deserialize(fs);
            }

            Assert.True(AreEqual(expected.Lst, actual));

            ListNode lastActualNode = actual.Head;
            while (lastActualNode.Prev != null)
                lastActualNode = lastActualNode.Prev;

            Assert.AreEqual(actual.Head.Rand, lastActualNode);
        }

        [Test]
        public void DeserializeList_ReferencedNodeBeforeReferencing_Success()
        {
            ListFacade expected;
            ListRand actual;
            string path = GetTempFilepath();
            expected = CreateFileValuesList();

            expected.Lst.Tail.Rand = expected.Lst.Head;

            using (var fs = new FileStream(path, FileMode.CreateNew))
            {
                expected.Serialize(fs);
                fs.Flush();
            }

            using (var fs = new FileStream(path, FileMode.Open))
            {
                actual = ListRand.Deserialize(fs);
            }

            Assert.True(AreEqual(expected.Lst, actual));

            Assert.AreEqual(actual.Tail.Rand, actual.Head);
        }

        [Test]
        public void DeserializeEmptyList_EmptyList_Success()
        {
            ListFacade expected = new ListFacade();
            ListRand actual;
            string path = GetTempFilepath();

            using (var fs = new FileStream(path, FileMode.CreateNew))
            {
                expected.Serialize(fs);
                fs.Flush();
            }

            using (var fs = new FileStream(path, FileMode.Open))
            {
                actual = ListRand.Deserialize(fs);
            }

            Assert.True(AreEqual(expected.Lst, actual));
        }
    }
}
