using System;
using System.Threading;

namespace Exp_3_2
{
    public interface IStack<T>
    {
        void Push(T item);
        bool TryPop(out T item);
        int Count { get; }
    }

    public class Node<T>
    {
        public Node<T> Next;
        public T Value;
    }

    public class ConcurrentStack<T> : IStack<T>
    {
        public int Count => Count_; // Сложность О(1)
        private int Count_;
        private readonly Node<T> LastNode;

        public ConcurrentStack()
        {
            LastNode = new Node<T>();
            Count_ = 0;
        }

        public void Push(T item)
        {
            var node = new Node<T>() { Value = item, Next = LastNode.Next };
            // Пока lastNode.Next будет неравен node.Next будет переприсваивание в цикле
            while (node.Next != Interlocked.CompareExchange(ref LastNode.Next, node, node.Next))
                node.Next = LastNode.Next;
            Interlocked.Increment(ref Count_);
        }

        public bool TryPop(out T result)
        {
            // Visual Studio посоветвала сделать так вместо "var node = new Node<T>()".
            // Наверное, нарушение стиля в данном случае лучше, чем занятие программой большего объёма памяти
            Node<T> node;
            do
            {
                node = LastNode.Next;
                if (node == null)
                {
                    result = default;
                    return false;
                }
            }
            while (node != Interlocked.CompareExchange(ref LastNode.Next, node.Next, node));
            Interlocked.Decrement(ref Count_);
            result = node.Value;
            return true;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // Небольшой пример
            var t = new ConcurrentStack<string>();
            var th1 = new Thread(() =>
            {
                for (var i = 1; i < 100; i++)
                    t.Push(i.ToString());
            });
            var th2 = new Thread(() =>
            {
                var r = "0";
                for (var i = 1; i < 120; i++)
                {
                    Console.WriteLine(t.Count);
                    Console.WriteLine(t.TryPop(out r));
                };
            });
            var th3 = new Thread(() =>
            {
                for (var i = 1; i < 100; i++)
                    t.Push(i.ToString());
            });
            var th4 = new Thread(() =>
            {
                var r = "0";
                for (var i = 1; i < 120; i++)
                {
                    Console.WriteLine(t.Count);
                    Console.WriteLine(t.TryPop(out r));
                };
            });
            th1.Start();
            th2.Start();
            th3.Start();
            th4.Start();
        }
    }
}
