using System;

namespace Quokka.Rollout
{
    public static class ConsoleTools
    {
        public static string MaskedEntry(string message)
        {
            string result = "";
            Console.WriteLine(message);

            while (true)
            {
                var key = Console.ReadKey(true);
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        Console.WriteLine();
                        return result;
                    case ConsoleKey.Escape:
                        return "";
                    case ConsoleKey.Backspace:
                        if (result.Length > 0)
                        {
                            result = result.Substring(0, result.Length - 1);

                            // clear last *
                            Console.SetCursorPosition(result.Length, Console.CursorTop);
                            Console.Write(" ");
                            Console.SetCursorPosition(result.Length, Console.CursorTop);
                        }
                        break;
                    default:
                        result += key.KeyChar;
                        Console.Write("*");
                        break;
                }
            }
        }

        public static void Write(string message, ConsoleColor color)
        {
            var currentColor = Console.ForegroundColor;
            try
            {
                Console.ForegroundColor = color;
                Console.Write(message);
            }
            finally
            {
                Console.ForegroundColor = currentColor;
            }
        }

        public static void WriteLine(string message, ConsoleColor color)
        {
            Write($"{message}{Environment.NewLine}", color);
        }

        public static void Info(string message)
        {
            WriteLine(message, ConsoleColor.Cyan);
        }

        public static void Warning(string message)
        {
            WriteLine(message, ConsoleColor.Yellow);

        }

        public static void Error(string message)
        {
            WriteLine(message, ConsoleColor.Red);
        }

        public static void Exception(Exception ex)
        {
            Error(ex.Message);
            WriteLine(ex.StackTrace, ConsoleColor.DarkYellow);
        }
    }
}
