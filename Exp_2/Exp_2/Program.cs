using System;
using System.Collections.Generic;
using System.Threading;

namespace Exp_2
{
    class Program
    {
        public interface IMultiLock
        {
            public IDisposable AcquireLock(params string[] keys);
        }

        public class MultiLockStorage : IDisposable
        {
            public List<string> keys;  // Местное хранилище ключей, которые нужно будет удалить при вызове Dispose
            public MultiLock origin;  // MultiLock, из которого был вызван этот класс
            public object locker = new object();

            public MultiLockStorage(List<string> keys_, MultiLock origin_)
            {
                lock (locker)
                {
                    keys = keys_;
                    origin = origin_;
                }
            }

            public void Dispose()
            {
                lock (locker)
                {
                    foreach (var key in keys)  // Удаление ключей в MultiLock
                        origin.keys.Remove(key);
                }
            }
        }

        public class MultiLock : IMultiLock
        {
            public List<string> keys = new List<string>();  // Место хранения ключей
            public object locker = new object();  // "Заглушка" для lock

            public MultiLock(params string[] keys_)
            {
                lock (locker)
                {
                    foreach (var key in keys_)  // Передача ключей классу
                        keys.Add(key);
                }
            }

            public IDisposable AcquireLock(params string[] keys_)
            {
                while (true)  // Проверяем введёные ключи на наличие их в keys
                {
                    lock (locker)
                    {
                        var tempStorage = new List<string>();
                        var wasCoincident = false;
                        foreach (var key in keys_)
                        {
                            if (keys.Contains(key))
                            {
                                wasCoincident = true;
                                break;
                            }
                            tempStorage.Add(key);
                        }
                        if (!wasCoincident) // Есть совпадение или нет, выходим из lock и процесссор может отдать приоритет другому потоку
                        {
                            keys.AddRange(tempStorage);
                            return new MultiLockStorage(tempStorage, this);
                        }
                    }
                }
            }
        }
        
        static void Main(string[] args)
        {
            /*
            Небольшой тест

            var y = new MultiLock("1k", "2k", "3k");
            var t1 = y.AcquireLock("4k", "5k");
            Console.WriteLine(y.keys);
            t1.Dispose();
            Console.WriteLine(y.keys);
            */
        }
    }
}
