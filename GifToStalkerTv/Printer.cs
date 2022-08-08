using System;
using System.Diagnostics;


namespace GifToStalkerTv
{
    public class Printer
    {
        public Printer()
        {
        }

        private void printLevelBracket(string level, ConsoleColor color)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(Process.GetCurrentProcess().ProcessName + ": ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("[");
            Console.ForegroundColor = color;
            Console.Write(level);
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("]");
        }
        public void warn(string msg)
        {
            printLevelBracket("WARN", ConsoleColor.DarkYellow);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void err(string msg)
        {
            printLevelBracket("ERROR", ConsoleColor.DarkRed);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void success(string msg)
        {
            printLevelBracket("SUCCESS", ConsoleColor.DarkGreen);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.White;
        }

        public void info(string msg)
        {
            printLevelBracket("INFO", ConsoleColor.DarkCyan);
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.White;
        }
    }
}
