using System;
using System.Collections.Generic;
using System.Threading;

namespace Exp_3
{
    public interface IStack<T>
    {
        void Push(T item);
        bool TryPop(out T item);
        int Count { get; }
    }

    public class ConcurrentStack<T> : IStack<T>
    {
        public int Count => Elements.Count;  // Elements.Count вычисляется за О(1), если судить по документации
        private readonly Stack<T> Elements;

        public ConcurrentStack()
        {
            Elements = new Stack<T>();
        }

        public void Push(T item)
        {
            Monitor.Enter(Elements);
            Elements.Push(item);
            Monitor.Exit(Elements);
        }

        public bool TryPop(out T item)
        {
            try
            {
                Monitor.Enter(Elements);
                item = Elements.Pop();
                Monitor.Exit(Elements);
                return true;
            }
            catch (InvalidOperationException)
            {
                item = default(T);
                return false;
            }
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
            th1.Start();
            th2.Start();
        }
    }
}