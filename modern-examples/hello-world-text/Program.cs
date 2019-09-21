using System;

namespace hello_world_text
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Clear();
            
            Console.WriteLine("Hello World!");
            Console.ReadKey();
        }
    }
}
