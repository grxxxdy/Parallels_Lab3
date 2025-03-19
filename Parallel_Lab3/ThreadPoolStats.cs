using System.Diagnostics;

namespace Parallel_Lab3;

public class ThreadPoolStats : IDisposable
{
    public int totalTasks;
    public int finishedTasks;
    public int totalDiscards;
    public List<double> tasksWaitTimes;
    public List<double> tasksExecutionTimes;
    public List<double> fullQueueTimes;

    private List<Stopwatch> _fullQueueStopwatches;
    private Stopwatch _totalTimeStopwatch;

    private long _totalExecutionTime;

    private Mutex _taskFinishMutex;
    private Mutex _tasksWaitMutex;
    private Mutex _tasksExecuteMutex;

    public ThreadPoolStats(int queueAmount)
    {
        totalTasks = 0;
        finishedTasks = 0;
        totalDiscards = 0;
        tasksWaitTimes = new List<double>();
        tasksExecutionTimes = new List<double>();
        fullQueueTimes = new List<double>();
        
        _fullQueueStopwatches = new List<Stopwatch>();
        _totalTimeStopwatch = new Stopwatch();
        
        _taskFinishMutex = new Mutex();
        _tasksWaitMutex = new Mutex();
        _tasksExecuteMutex = new Mutex();

        for (int i = 0; i < queueAmount; i++)
        {
            fullQueueTimes.Add(0);
            _fullQueueStopwatches.Add(new Stopwatch());
        }
    }

    public void StartExecutionTimeMeasure()
    {
        _totalTimeStopwatch.Restart();
    }
    
    public void StopExecutionTimeMeasure()
    {
        _totalTimeStopwatch.Stop();
        _totalExecutionTime = _totalTimeStopwatch.ElapsedMilliseconds;
    }

    public void AddFinishedTask()
    {
        _taskFinishMutex.WaitOne();
        finishedTasks++;
        _taskFinishMutex.ReleaseMutex();
    }

    public void RecordTaskWaitTime(Stopwatch taskStopwatch)
    {
        taskStopwatch.Stop();
        
        _tasksWaitMutex.WaitOne();
        tasksWaitTimes.Add(taskStopwatch.ElapsedMilliseconds);
        _tasksWaitMutex.ReleaseMutex();
    }

    public void RecordTaskExecutionTime(int executionTime)
    {
        _tasksExecuteMutex.WaitOne();
        tasksExecutionTimes.Add(executionTime);
        _tasksExecuteMutex.ReleaseMutex();
    }
    
    public void QueueFullStart(int index)
    {
        _fullQueueStopwatches[index].Restart();
    }

    public void QueueFullStop(int index)
    {
        _fullQueueStopwatches[index].Stop();

        fullQueueTimes[index] += _fullQueueStopwatches[index].ElapsedMilliseconds;
    }

    public void PrintStats()
    {
        Console.WriteLine("____________________________________________");
        Console.WriteLine($"Average task wait time: {(tasksWaitTimes.Any() ? tasksWaitTimes.Average() : 0)}");
        Console.WriteLine($"Average task execution time: {(tasksExecutionTimes.Any() ? tasksExecutionTimes.Average() : 0)}");
        Console.WriteLine($"Maximum time of a queue being full: {(fullQueueTimes.Any() ? fullQueueTimes.Max() : 0)}");
        Console.WriteLine($"Minimum time of a queue being full: {(fullQueueTimes.Any() ? fullQueueTimes.Min() : 0)}");
        Console.WriteLine($"Tasks discarded: {totalDiscards}/{totalTasks}");
        Console.WriteLine("____________________________________________");
        Console.WriteLine($"Total execution time: {_totalExecutionTime}");
        Console.WriteLine("____________________________________________");
    }

    public void Dispose()
    {
        _taskFinishMutex.Dispose();
        _tasksWaitMutex.Dispose();
        _tasksExecuteMutex.Dispose();
    }
}