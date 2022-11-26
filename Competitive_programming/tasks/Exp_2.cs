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
            public Mutex mutexObject = new Mutex();

            public MultiLockStorage(List<string> keys_, MultiLock origin_)
            {
                keys = keys_;
                origin = origin_;
            }

            public void Dispose()
            {
                mutexObject.WaitOne();
                foreach (var key in keys)  // Удаление ключей в MultiLock
                    origin.keys.Remove(key);
                mutexObject.ReleaseMutex();
            }
        }

        public class MultiLock : IMultiLock
        {
            public List<string> keyOptions = new List<string>(); // Все доступные варианты ключа
            public List<string> keys = new List<string>();  // Место хранения ключей
            public Mutex mutexObject = new Mutex();

            public MultiLock(params string[] keys_)
            {
                foreach (var key in keys_)  // Передача ключей классу
                    keyOptions.Add(key);
            }

            public IDisposable AcquireLock(params string[] keys_)
            {
                while (true)  // Проверяем введёные ключи на наличие их в keys
                {
                    var tempStorage = new List<string>();
                    var wasCoincident = false;
                    mutexObject.WaitOne();
                    foreach (var key in keys_)
                    {
                        if (!keyOptions.Contains(key))
                            throw new ArgumentException("Выход за предел доступных для блокирования ключей");
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
                        mutexObject.ReleaseMutex();
                        return new MultiLockStorage(tempStorage, this);
                    }
                    mutexObject.ReleaseMutex();
                }
            }
        }

        static void Main(string[] args)
        {
            /*
            Небольшой тест
            
            var y = new MultiLock("1k", "2k", "3k");
            // var t1 = y.AcquireLock("4k", "5k"); // Ошибка 
            // var t1 = y.AcquireLock("3k", "4k");  // Ошибка
            var t1 = new Thread(() => 
            {
                var a = y.AcquireLock("1k", "2k", "3k");
                Console.WriteLine("Поток 1 засыпает");
                Thread.Sleep(2000);
                Console.WriteLine("Поток 1 проснулся");
                a.Dispose();
            });
            var t2 = new Thread(() =>
            {
                var b = y.AcquireLock("3k", "2k");
                Console.WriteLine("Поток 2 засыпает");
                Thread.Sleep(2000);
                Console.WriteLine("Поток 2 проснулся");
                b.Dispose();
            });
            var t3 = new Thread(() =>
            {
                var c = y.AcquireLock("0k", "3k");
                Console.WriteLine("Поток 3 засыпает");
                Thread.Sleep(2000);
                Console.WriteLine("Поток 3 проснулся");
                c.Dispose();
            });
            t1.Start();
            t2.Start();
            // t3.Start(); // Ошибка
            Console.WriteLine(y.keys);
            Thread.Sleep(5000);
            Console.WriteLine("End");
            */
        }
    }
}
