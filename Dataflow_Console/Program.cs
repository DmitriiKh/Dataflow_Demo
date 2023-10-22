namespace Dataflow_Console
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Running insurance example.");
            Insurance_example.Run();

            Console.WriteLine();

            Console.WriteLine("Running NAudio example.");
            NAudio_example.Run();
        }
    }
}