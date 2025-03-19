namespace Parallel_Lab3;

class Program
{
    static void Main(string[] args)
    {
        int queueAmnt = 3, threadsPerQueue = 2, queueCapacity = 10, tasksAmount = 100;
        
        Console.WriteLine("____________________________________________");
        Console.WriteLine($"Queue amount: {queueAmnt}.\nThreads per queue: {threadsPerQueue}.\nQueue capacity: {queueCapacity}.\nAmount of tasks: {tasksAmount}");
        Console.WriteLine("____________________________________________");

        ThreadPoolStats stats = new ThreadPoolStats(queueAmnt);
        ThreadPool threadPool = new ThreadPool(queueAmnt, threadsPerQueue, queueCapacity, stats);

        for (int i = 0; i < tasksAmount; i++)
        {
            threadPool.EnqueueTask(WaitForRandomSeconds, i);
            Thread.Sleep(10);   // Test for more beautiful output
        }

        threadPool.WaitForTasksAndFinish(tasksAmount);
        
        stats.PrintStats();
    }

    static int WaitForRandomSeconds()
    {
        Random rand = new Random();

        int timeToWait = rand.Next(6, 12) * 1000;
        
        Thread.Sleep(timeToWait);

        return timeToWait;
    }
}