using System;
using System.Threading;
using System.Collections.Generic;

namespace CustomThreadPool
{
    public class DotNetThreadPoolWrapper : IThreadPool
    {
        private long processedTask = 0L;

        public void EnqueueAction(Action action)
        {
            ThreadPool.UnsafeQueueUserWorkItem(delegate
            {
                action.Invoke();
                Interlocked.Increment(ref processedTask);
            }, null);
        }

        public long GetTasksProcessedCount() => processedTask;
    }

    public class MyThreadPool : IThreadPool
    {
        private long tasksProcessedCount = 0; // Кол-во выполненых заданий
        private readonly int threadsCount; // Кол-во потоков
        private readonly List<object> lockers = new List<object>(); // Заглушки для потоков
        private readonly List<Action> works = new List<Action>(); // Биекция работа - поток
        private readonly Queue<Action> actions = new Queue<Action>(); // Очередь задач

        public MyThreadPool()
        {
            threadsCount = 100; // 100 - число из головы
            for (var i = 0; i < threadsCount; i++)
            {
                lockers.Add(new object());
                works.Add(null);
                var thread = new Thread((threadNumber) =>
                {
                    while (true) // Бесконечный цикл взятия первой задачи из очереди и ухода потока на покой после её выполнения
                    {
                        Monitor.Enter(lockers[(int)threadNumber]);
                        works[(int)threadNumber] = null;
                        Monitor.Wait(lockers[(int)threadNumber]); // Блокировка поктока при его создании и после завершения работы
                        Monitor.Exit(lockers[(int)threadNumber]);
                        works[(int)threadNumber].Invoke();
                        Interlocked.Increment(ref tasksProcessedCount);
                    }
                });
                thread.Start(i);
            }
            var mainThread = new Thread(() => { DoAction(); }); // Поток отвечающий за "Пульс" для других потоков
            mainThread.Start();
        }

        public void DoAction()  // Метод через который потоки получают "Пульс" для выполнения задач
        {
            int actionsCount; // Чтобы в бесконечном цикле много раз не заводить одну и ту же переменную, вынес её сюда
            while (true) // В бесконечном цикле проверяет, нужно ли выполнять задания. И, если нужно, то отдаёт то отдаёт по задаче первому свободному потоку
            {
                actionsCount = actions.Count;
                if (actionsCount == 0)
                    continue;
                for (var i = 0; i < threadsCount; i++)
                {
                    if (works[i] != null)
                        continue;
                    Monitor.Enter(actions);
                    works[i] = actions.Dequeue();
                    Monitor.Exit(actions);
                    Monitor.Enter(lockers[i]);
                    Monitor.Pulse(lockers[i]);
                    Monitor.Exit(lockers[i]);
                    Interlocked.Decrement(ref actionsCount);
                    if (actionsCount == 0)
                        break;
                }
            }
        }
        public void EnqueueAction(Action action)  // Добавления задачи в очередь задач
        {
            Monitor.Enter(actions);
            actions.Enqueue(action);
            Monitor.Exit(actions);
        }

        public long GetTasksProcessedCount() // Вывод кол-вa выполненых задач
        {
            return tasksProcessedCount;
        }
    }
}