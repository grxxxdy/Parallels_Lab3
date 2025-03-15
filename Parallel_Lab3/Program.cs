namespace Parallel_Lab3;

class Program
{
    static void Main(string[] args)
    {
        ThreadPool threadPool = new ThreadPool(3, 2, 10);

        for (int i = 0; i < 10; i++)
        {
            threadPool.EnqueueTask(WaitForRandomSeconds, i);
        }
    }

    static void WaitForRandomSeconds()
    {
        Random rand = new Random();
        
        Thread.Sleep(rand.Next(6, 12) * 1000);
    }
}