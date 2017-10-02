using System.IO;
using System;
using System.Collections.Generic;
using System.Web;

namespace ListSerialization
{
    // multithreaded environment isn't a requrement, so no single threaded usage check
    internal class ListSerializer
    {
        private class StringsEscaper // let's pretend that I wrote my own instead of using some standard
        {
            public static string Escape(string str)
            {
                return str == null
                    ? "NULL"
                    : HttpUtility.UrlEncode(str); // complete URL escaping isn't necessary, it's just simplier and safier, but affects size
            }

            public static string UnEscape(string str)
            {
                return str == null || str.Equals("NULL")
                    ? null
                    : HttpUtility.UrlDecode(str);
            }
        }

        private static string _delim = " "; // that's possible only because we use URI escaper and it will encode spaces
        private static char _sep = ':';

        private readonly ListRand _serializedList;
        private int _index;

        private Dictionary<object, string> _objectToString;
        private Dictionary<object, int> _objectToIndex;

        private ListSerializer() { }

        public ListSerializer(ListRand lst)
        {
            if (lst == null) throw new ArgumentNullException(nameof(lst));
            _serializedList = lst;

            _objectToString = new Dictionary<object, string>();
            _objectToIndex = new Dictionary<object, int>();

            _index = 1;
        }

        public void Serialize(FileStream s)
        {
            if (_serializedList.Count == 0)
                return;

            var sw = new StreamWriter(s);        // we don't need to dispose as we don't own this resource
            var tmp = _serializedList.Head;
            while (tmp != null)
            {
                sw.WriteLine(FormatNode(tmp));  // I need StreamWrite just to use WriteLine, nothing more; it's easier 
                tmp = tmp.Prev;                 // as long as Rand can be only inside the list (it is reachable through Next/Prev), we have no problem here
            }

            sw.Flush();
        }

        public static ListRand Deserealize(string str)
        {
            if (string.IsNullOrEmpty(str))
                return new ListRand();         // deserialization of empty lists 

            var serializedNodes = str.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            var map = new Dictionary<string, ValueTuple<string, string, string, string>>(); // using ValueTuple just because it is not required to write generic serializer

            foreach (var serializedNode in serializedNodes)
            {
                var parts = serializedNode.Split(new[] { _delim }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 5)
                {
                    // let's just throw an exception and write a good unit test
                    throw new InvalidDataException("There are less parts in serialization data than we expect");
                }

                // at some moment we will use more memory than we really need, optimisation isn't performed
                var id = parts[0].Split(_sep)[1];
                var data = StringsEscaper.UnEscape(parts[1].Split(_sep)[1]);
                var rndIdx = parts[2].Split(_sep)[1];
                var prevIdx = parts[3].Split(_sep)[1];
                var nextIdx = parts[4].Split(_sep)[1];

                map[id] = ( data, rndIdx, prevIdx, nextIdx );
            }

            var lst = new ListRand();
            var restoredNodes = new Dictionary<string, ListNode>();
            var restoredNode = new ListNode();

            foreach (var kv in map)
            {
                restoredNode = RestoreNode(kv.Key, map, restoredNodes);
                restoredNode.Rand = RestoreNode(kv.Value.Item2, map, restoredNodes);
                restoredNode.Prev = RestoreNode(kv.Value.Item3, map, restoredNodes);
                restoredNode.Next = RestoreNode(kv.Value.Item4, map, restoredNodes);
            }


            lst.Head = restoredNode;
            while (lst.Head.Next != null) // guess we can get rid of this, but as there is no requirement of high performance, no optimisation performed
                lst.Head = lst.Head.Next;

            lst.Tail = lst.Head.Prev;

            lst.Count = map.Count;

            return lst;
        }

        public static ListNode RestoreNode(string idx, Dictionary<string, ValueTuple<string, string, string, string>> map, Dictionary<string, ListNode> restored)
        {
            // deserializes node from string data or takes it from already deserialized
            ListNode node;
            if (idx.Equals("0"))
                return null;

            if (!restored.TryGetValue(idx, out node))
            {
                node = new ListNode()
                {
                    Data = map[idx].Item1
                };
                restored[idx] = node;
            }
            return node;
        }

        private string FormatNode(ListNode node)
        {
            int idx;
            if (!_objectToIndex.TryGetValue(node, out idx))
            {
                idx = _index += 1;
                _objectToIndex[node] = idx;
            }
            else // oh, we already have this value, must be referenced before
            {
                return _objectToString[node];
            }

            int rndIdx = 0;
            if (node.Rand != null)
            {
                if (!_objectToIndex.TryGetValue(node.Rand, out rndIdx))
                {
                    FormatNode(node.Rand); // okay, we have recursion here and can fuck everything up; we can solve this, but it's a trade-off and without clear requiriments this optimisation is premature
                    _objectToIndex.TryGetValue(node.Rand, out rndIdx);
                }
            }

            int prevIdx = 0;
            if (node.Prev != null) // as I don't know implementation of Add method, I assume that Prev could be null
            {
                if (!_objectToIndex.TryGetValue(node.Prev, out prevIdx))
                {
                    FormatNode(node.Prev);
                    _objectToIndex.TryGetValue(node.Prev, out prevIdx);
                }
            }

            int nextIdx = 0;
            if (node.Next != null) // as I don't know implementation of Add method, I assume that Next could be null
            {
                if (!_objectToIndex.TryGetValue(node.Next, out nextIdx))
                {
                    FormatNode(node.Next);
                    _objectToIndex.TryGetValue(node.Next, out nextIdx);
                }
            }

            // using of predefined ID, DATA, RAND, etc field names is more safier than using actual names of fields, as they could changed with time and broke back compatibility
            string result = $"ID{_sep}{idx}{_delim}DATA{_sep}{StringsEscaper.Escape(node.Data)}{_delim}RAND{_sep}{rndIdx}{_delim}PREV{_sep}{prevIdx}{_delim}NEXT{_sep}{nextIdx}";
            _objectToString[node] = result;

            return result;
        }
    }
}
