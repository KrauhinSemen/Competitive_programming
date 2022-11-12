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

        class MultiLockStorage : IDisposable
        {
            public List<string> keys;
            public MultiLock origin;
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
                    foreach (var key in keys)
                        origin.keys.Remove(key);
                }
            }
        }

        class MultiLock : IMultiLock
        {
            public List<string> keys = new List<string>();
            public object locker = new object();

            public MultiLock(params string[] keys_)
            {
                lock (locker)
                {
                    foreach (var key in keys_)
                        keys.Add(key);
                }
            }

            public IDisposable AcquireLock(params string[] keys_)
            {
                while (true)
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
                        if (!wasCoincident) 
                            return new MultiLockStorage(tempStorage, this);
                    }
                }
            }
        }

        static void Main(string[] args)
        {

        }
    }
}
