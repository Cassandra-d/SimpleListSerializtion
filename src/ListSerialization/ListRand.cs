using System.IO;

namespace ListSerialization
{
    public class ListNode
    {
        public ListNode Prev;
        public ListNode Next;
        public ListNode Rand; // произвольный элемент внутри списка
        public string Data;
    }

    public class ListRand
    {
        public ListNode Head;
        public ListNode Tail;
        public int Count;

        public void Serialize(FileStream s)
        {
            new ListSerializer(this).Serialize(s);
        }

        // I cannot imagine how a list should deserialize itself into itself;
        // this design isn't obvious, I'll just change it a little
        public static ListRand Deserialize(FileStream s)
        {
            return ListSerializer.Deserealize(new StreamReader(s).ReadToEnd()); // simplification to use ReadToEnd; again we don't own this res, so no dispose
        }
    }
}
