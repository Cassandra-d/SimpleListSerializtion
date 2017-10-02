using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;

namespace ListSerialization
{
    // just my little helper
    public class ListFacade : IEnumerable<ListNode>
    {
        private ListRand _lst;
        public ListRand Lst { get => _lst; set => _lst = value; }

        public ListFacade()
        {
            Lst = new ListRand();
        }

        public ListFacade(ListRand lst)
        {
            Lst = lst ?? throw new ArgumentNullException(nameof(lst));
        }

        public void Add(string str)
        {
            Lst.Tail = Lst.Head;
            Lst.Head = new ListNode()
            {
                Data = str,
                Next = null,
                Prev = Lst.Tail,
                Rand = null
            };

            if (Lst.Tail != null)
                Lst.Tail.Next = Lst.Head;

            Lst.Count += 1;

            //AssignRandom();
        }

        /// <summary>
        /// We are zero-based here
        /// </summary>
        /// <param name="n">Position</param>
        /// <returns>Node at position</returns>
        public ListNode ElementAt(int n)
        {
            if (Lst.Count == 0)
                throw new InvalidOperationException("List is empty");
            if (Lst.Count < n - 1)
                throw new IndexOutOfRangeException(nameof(Lst));

            if (n == 0)
                return Lst.Head;

            var tmp = Lst.Tail;
            n -= 1;

            while (n-- > 0)
                tmp = tmp.Prev;

            return tmp;
        }

        private void AssignRandom()
        {
            switch (Lst.Count)
            {
                case 0:
                case 1:
                    break;
                case 4:
                    ElementAt(1).Rand = ElementAt(2);
                    break;
                case 6:
                    ElementAt(2).Rand = ElementAt(3);
                    break;
                case 7: // circular references !
                    ElementAt(5).Rand = ElementAt(6);
                    ElementAt(6).Rand = ElementAt(5);
                    break;
                case 10:
                    ElementAt(8).Rand = ElementAt(7);
                    break;
                default:
                    break;
            }
        }

        public IEnumerator<ListNode> GetEnumerator()
        {
            if (Lst.Head == null)
                yield break;

            yield return Lst.Head;

            var tmp = Lst.Tail;

            if (tmp == null)
                yield break;

            while (tmp != null)
            {
                yield return tmp;
                tmp = tmp.Prev;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Serialize(FileStream s)
        {
            Lst.Serialize(s);
        }

        public ListRand Deserialize(FileStream s)
        {
            return ListRand.Deserialize(s);
        }
    }
}
