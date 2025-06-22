﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ServerShared.Logging;

namespace Server
{
    public static class ConsoleManager
    {
        public static Action<string> OnInput;

        private static Thread inputThread;
        private static string currentInput = string.Empty;

        private static readonly Stack<ConsoleColor> foregroundColorStack = new Stack<ConsoleColor>();
        private static readonly Stack<ConsoleColor> backgroundColorStack = new Stack<ConsoleColor>();
        private static readonly List<string> history = new List<string>();
        private static int historyPosition = 0; // 0 = oldest, higher = newer

        private static readonly object consoleLock = new object();
        private static bool isRunning;

        public static void Initialize()
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            Console.Clear();

            Logger.LogMessageReceived += OnLogMessageReceived;
            isRunning = true;
            inputThread = new Thread(DoInputThread);
            inputThread.Start();

            PushForegroundColor(ConsoleColor.DarkGreen); // Set default color to dark green
        }

        public static void Destroy()
        {
            Logger.LogMessageReceived -= OnLogMessageReceived;
            isRunning = false;
            PopForegroundColor();

            foregroundColorStack.Clear();
            backgroundColorStack.Clear();
            history.Clear();
        }

        public static void Clear()
        {
            Console.Clear();
            RedrawInput();
        }

        private static void OnLogMessageReceived(LogMessageReceivedEventArgs args)
        {
            lock (consoleLock)
            {
                switch (args.Type)
                {
                    case LogMessageType.Info:
                        PushForegroundColor(ConsoleColor.Gray);
                        WriteLine(args.Message);
                        PopForegroundColor();
                        break;
                    case LogMessageType.Debug:
                        PushForegroundColor(ConsoleColor.Yellow);
                        WriteLine(args.Message);
                        PopForegroundColor();
                        break;
                    case LogMessageType.Warning:
                        PushForegroundColor(ConsoleColor.DarkYellow);
                        WriteLine(args.Message);
                        PopForegroundColor();
                        break;
                    case LogMessageType.Error:
                        PushForegroundColor(ConsoleColor.Red);
                        WriteLine(args.Message);
                        PopForegroundColor();
                        break;
                    case LogMessageType.Exception:
                        PushForegroundColor(ConsoleColor.Red);

                        if (args.Message != null)
                            WriteLine(args.Message);

                        WriteException(args.Exception);

                        PopForegroundColor();
                        break;

                    case LogMessageType.Custom:
                        var color = args.CustomColor ?? ConsoleColor.Gray;
                        PushForegroundColor(color);
                        WriteLine(args.Message);
                        PopForegroundColor();
                        break;
                }
            }
        }

        private static void WriteLine(object msg)
        {
            lock (consoleLock)
            {
                Console.WriteLine(msg);
                RedrawInput();
            }
        }

        private static void WriteException(Exception exception)
        {
            lock (consoleLock)
            {
                Console.WriteLine(exception);

                if (exception.InnerException != null)
                {
                    PushForegroundColor(ConsoleColor.DarkRed);
                    Console.Write("Inner: ");
                    PopForegroundColor();
                    WriteException(exception.InnerException);
                }

                RedrawInput();
            }
        }

        private static void DoInputThread()
        {
            while (isRunning)
            {
                if (!WaitForKey(100, out var keyInfo))
                    continue;

                if (keyInfo.Key == ConsoleKey.Backspace)
                {
                    OnBackspace();
                }
                else if (keyInfo.Key == ConsoleKey.Enter)
                {
                    OnEnter();
                }
                else if (keyInfo.Key == ConsoleKey.Escape)
                {
                    currentInput = "";
                }
                else if (keyInfo.Key == ConsoleKey.UpArrow)
                {
                    if (history.Count > 0)
                    {
                        historyPosition -= 1;

                        if (historyPosition < 0)
                            historyPosition = 0;

                        currentInput = history[historyPosition];
                    }
                }
                else if (keyInfo.Key == ConsoleKey.DownArrow)
                {
                    if (history.Count > 0)
                    {
                        historyPosition += 1;

                        if (historyPosition >= history.Count)
                        {
                            historyPosition = history.Count;
                            currentInput = "";
                        }
                        else
                        {
                            currentInput = history[historyPosition];
                        }
                    }
                }
                else if (keyInfo.KeyChar != 0)
                {
                    currentInput += keyInfo.KeyChar;
                }

                RedrawInput();
            }
        }

        private static bool WaitForKey(int ms, out ConsoleKeyInfo keyInfo)
        {
            int delay = 0;
            while (delay < ms)
            {
                if (Console.KeyAvailable)
                {
                    keyInfo = Console.ReadKey(true);
                    return true;
                }
                Thread.Sleep(15);
                delay += 15;
            }

            keyInfo = default;
            return false;
        }

        private static void OnBackspace()
        {
            if (currentInput.Length > 0)
            {
                currentInput = currentInput.Substring(0, currentInput.Length - 1);
            }
        }

        private static void OnEnter()
        {
            if (currentInput.Length <= 0)
                return;

            lock (consoleLock)
            {
                int inputLine = Console.CursorTop;
                Console.SetCursorPosition(0, inputLine);

                PushForegroundColor(ConsoleColor.Green);
                Console.Write(currentInput.PadRight(Console.WindowWidth - 1));
                PopForegroundColor();

                Console.WriteLine();

                OnInput?.Invoke(currentInput);

                if (history.Count > 1000)
                    history.RemoveAt(0);

                history.Add(currentInput);
                historyPosition = history.Count;
                currentInput = string.Empty;
            }

            RedrawInput();
        }

        private static void RedrawInput()
        {
            lock (consoleLock)
            {
                Console.CursorVisible = false; // Prevent flickering cursor

                int inputLine = Console.CursorTop;
                Console.SetCursorPosition(0, inputLine);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, inputLine);

                string input = currentInput;

                if (input.Length > Console.BufferWidth - 1)
                {
                    input = input.Substring(input.Length - (Console.BufferWidth - 1));
                }

                PushForegroundColor(ConsoleColor.Green);
                Console.Write($"{input}");
                PopForegroundColor();

                Console.CursorVisible = true;
            }
        }

        public static void ClearLines(int numLines)
        {
            int targetTop = Console.CursorTop - numLines;
            if (targetTop < 0) targetTop = 0;

            Console.SetCursorPosition(0, targetTop);

            for (int i = 0; i < numLines; i++)
            {
                Console.Write(new string(' ', Console.WindowWidth));
                if (i < numLines - 1)
                    Console.WriteLine();
            }

            Console.SetCursorPosition(0, targetTop);
        }

        private static void PushForegroundColor(ConsoleColor color)
        {
            foregroundColorStack.Push(Console.ForegroundColor);
            Console.ForegroundColor = color;
        }

        private static void PopForegroundColor()
        {
            Console.ForegroundColor = foregroundColorStack.Pop();
        }

        private static void PushBackgroundColor(ConsoleColor color)
        {
            backgroundColorStack.Push(Console.BackgroundColor);
            Console.BackgroundColor = color;
        }

        private static void PopBackgroundColor()
        {
            Console.BackgroundColor = backgroundColorStack.Pop();
        }
    }
}
