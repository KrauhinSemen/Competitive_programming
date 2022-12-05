public class MyThreadPool : IThreadPool
    {
        private long tasksProcessedCount = 0; // Кол-во выполненых заданий
        private readonly int threadsCount; // Кол-во потоков
        private readonly List<bool> threadOccupancy = new List<bool>(); // Булевое значения занятости потока на данный момент
        private readonly List<object> lockers = new List<object>(); // Заглушки для потоков
        private readonly Queue<Action> actions = new Queue<Action>(); // Очередь задач

        public MyThreadPool()//int threadsCount_)
        {
            threadsCount = 100; // threadsCount_;
            for (var i = 0; i < 100; i++)
            {
                threadOccupancy.Add(true);
                lockers.Add(new object());
                var thread = new Thread((threadNumber) =>
                {
                    Action action; // чтобы в бесконечном цикле много раз не заводить одну и ту же переменную, вынес её сюда
                    while (true) // Бесконечный цикл взятия вервой задачи из очереди и ухода потока на покой после её выполнения
                    {
                        threadOccupancy[(int)threadNumber] = false;
                        lock (lockers[(int)threadNumber])
                        {
                            Monitor.Wait(lockers[(int)threadNumber]); // Блокировка поктока при его создании и после завершения работы
                        }
                        lock (actions)  // Здесь, наверное, можно и удалить lock, но не могу с уверенностью заявить, что он здесь не нужен
                        {
                            action = actions.Dequeue();
                        }
                        action.Invoke();
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
            int actionsCount; // чтобы в бесконечном цикле много раз не заводить одну и ту же переменную, вынес её сюда
            while (true) // В бесконечном цикле проверяет, нужно ли выполнять задания. И, если нужно, то отдаёт то отдаёт по задаче первому свободному потоку
            {
                actionsCount = actions.Count;
                if (actionsCount == 0)
                    continue;
                for (var i = 0; i < threadsCount; i++)
                {
                    if (threadOccupancy[i])
                        continue;
                    threadOccupancy[i] = true;
                    lock (lockers[i])
                    {
                        Monitor.Pulse(lockers[i]);
                    }
                    Interlocked.Decrement(ref actionsCount);
                    if (actionsCount == 0)
                        break;
                }
            }
        }
        public void EnqueueAction(Action action)  // Добавления задачи в очередь задач
        {
            lock (actions)
            {
                actions.Enqueue(action);
            }
        }

        public long GetTasksProcessedCount() // Вывод кол-во выполненых задач
        {
            return tasksProcessedCount;
        }
    }