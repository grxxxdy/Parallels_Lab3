﻿using System.Diagnostics;

namespace Parallel_Lab3;

public class ThreadPool : IDisposable
{
    private List<Queue<(int, Func<int>, Stopwatch)>> _queues;       // int - task index      Func - task itself      Stopwatch - individual for each task for wait time measure
    private List<Thread> _workers;
    
    private bool _shouldInterrupt;
    private int _queueCapacity;
    
    private List<Mutex> _queueLocks;
    private Mutex _consoleMutex;

    private ThreadPoolStats _stats;
    
    public ThreadPool(int queueAmount, int threadPerQueue, int queueCapacity, ThreadPoolStats stats)
    {
        _shouldInterrupt = false;
        _queues = new List<Queue<(int, Func<int>, Stopwatch)>>();
        _workers = new List<Thread>();
        _queueCapacity = queueCapacity;
        _queueLocks = new List<Mutex>();
        _consoleMutex = new Mutex();
        _stats = stats;
        
        for (int i = 0; i < queueAmount; i++)
        {
            // Initialize queue and it's mutex
            _queues.Add(new Queue<(int, Func<int>, Stopwatch)>(queueCapacity));
            
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

    public void EnqueueTask(Func<int> task, int taskId)
    {
        _stats.totalTasks++;
        
        Random rand = new Random();

        int startIndex = rand.Next(0, _queues.Count);        
        
        // Selected random queue, if it's full -> iterate through others
        for (int i = 0; i < _queues.Count; i++)
        {
            int index = (startIndex + i) % _queues.Count;

            _queueLocks[index].WaitOne();                   // -----LOCK QUEUE-----

            if (_queues[index].Count < _queueCapacity)
            {
                _queues[index].Enqueue((taskId, task, Stopwatch.StartNew()));

                if (_queues[index].Count == _queueCapacity)         // if queue became full then we need to start measuring time
                {
                    _stats.QueueFullStart(index);
                }
                
                _queueLocks[index].ReleaseMutex();          // -----UNLOCK QUEUE-----

                _consoleMutex.WaitOne();
                Console.WriteLine($"[Queue {index}] Task {taskId} has been enqueued.");
                _consoleMutex.ReleaseMutex();
                
                return;
            }
            
            _queueLocks[index].ReleaseMutex();              // -----UNLOCK QUEUE-----
        }
        
        _consoleMutex.WaitOne();
        Console.WriteLine("All queues are full.");
        _consoleMutex.ReleaseMutex();

        _stats.totalDiscards++;
    }

    private void ProcessTasks(int queueIndex)
    {
        while (!_shouldInterrupt)
        {
            _queueLocks[queueIndex].WaitOne();                  // -----LOCK QUEUE-----
            
            if(_queues[queueIndex].Count > 0)
            {
                var (taskIndex, task, waitTimeStopwatch) = _queues[queueIndex].Dequeue();

                if (_queues[queueIndex].Count == _queueCapacity - 1)    // if queue was full, then now we need to stop time measure
                {
                    _stats.QueueFullStop(queueIndex);
                }
                
                _queueLocks[queueIndex].ReleaseMutex();         // -----UNLOCK QUEUE-----
                
                _stats.RecordTaskWaitTime(waitTimeStopwatch);   // Waiting is finished so recrd the time
                
                _consoleMutex.WaitOne();
                Console.WriteLine($"Task {taskIndex} has started.");
                _consoleMutex.ReleaseMutex();
                
                // Execute and measure
                int executionTime = task.Invoke();
                _stats.RecordTaskExecutionTime(executionTime);
                
                _consoleMutex.WaitOne();
                Console.WriteLine($"Task {taskIndex} has completed.");
                _consoleMutex.ReleaseMutex();

                _stats.AddFinishedTask();
            }
            else
            {
                _queueLocks[queueIndex].ReleaseMutex();         // -----UNLOCK QUEUE-----
            }
        }
    }

    public void WaitForTasksAndFinish(int taskAmount)
    {
        while (true)
        {
            if (_stats.finishedTasks + _stats.totalDiscards >= taskAmount)
            {
                Dispose();
                return;
            }
            
            Thread.Sleep(100);
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