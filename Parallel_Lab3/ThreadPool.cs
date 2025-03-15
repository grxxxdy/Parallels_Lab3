namespace Parallel_Lab3;

public class ThreadPool : IDisposable
{
    private List<Queue<(int, Action)>> _queues;
    private List<Thread> _workers;
    
    private bool _shouldInterrupt;
    private int _queueCapacity;
    
    private List<Mutex> _queueLocks;
    private Mutex _consoleMutex;
    
    public ThreadPool(int queueAmount, int threadPerQueue, int queueCapacity)
    {
        _shouldInterrupt = false;
        _queues = new List<Queue<(int, Action)>>();
        _workers = new List<Thread>();
        _queueCapacity = queueCapacity;
        _queueLocks = new List<Mutex>();
        _consoleMutex = new Mutex();
        
        for (int i = 0; i < queueAmount; i++)
        {
            // Initialize queue and it's mutex
            _queues.Add(new Queue<(int, Action)>(queueCapacity));
            
            _queueLocks.Add(new Mutex());
            
            // Initialize threads for the queue
            for (int j = 0; j < threadPerQueue; j++)
            {
                int threadId = i;
                var worker = new Thread(() => ProcessTasks(threadId));
                
                _workers.Add(worker);
                worker.Start();
            }
        }
    }

    public void EnqueueTask(Action task, int taskId)
    {
        Random rand = new Random();

        int index = rand.Next(0, _queues.Count);        
        
        // Selected random queue, if it's full -> iterate through others
        for (int i = 0; i < _queues.Count; i++)
        {
            index = (index + i) % _queues.Count;

            _queueLocks[index].WaitOne();

            if (_queues[index].Count < _queueCapacity)
            {
                _queues[index].Enqueue((taskId, task));
                _queueLocks[index].ReleaseMutex();

                _consoleMutex.WaitOne();
                Console.WriteLine($"[Queue {index}] Task {taskId} has been enqueued.");
                _consoleMutex.ReleaseMutex();
                
                return;
            }
            
            _queueLocks[index].ReleaseMutex();
        }
        
        _consoleMutex.WaitOne();
        Console.WriteLine("All queues are full.");
        _consoleMutex.ReleaseMutex();
    }

    private void ProcessTasks(int queueIndex)
    {
        while (!_shouldInterrupt)
        {
            _queueLocks[queueIndex].WaitOne();
            
            if(_queues[queueIndex].Count > 0)
            {
                var task = _queues[queueIndex].Dequeue();
                _queueLocks[queueIndex].ReleaseMutex();
                
                _consoleMutex.WaitOne();
                Console.WriteLine($"Task {task.Item1} has started.");
                _consoleMutex.ReleaseMutex();
                
                task.Item2.Invoke();
                
                _consoleMutex.WaitOne();
                Console.WriteLine($"Task {task.Item1} has completed.");
                _consoleMutex.ReleaseMutex();
            }
            else
            {
                _queueLocks[queueIndex].ReleaseMutex();
            }
        }
    }

    public void Dispose()
    {
        _shouldInterrupt = true;

        foreach (var worker in _workers)
        {
            worker.Join();
        }

        foreach (var mutex in _queueLocks)
        {
            mutex.Dispose();
        }
    }
}