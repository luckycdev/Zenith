using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using ServerShared.Logging;

namespace Server
{
    public static class ConsoleManager
    {
        //TODO: console input line flickers on windows
        //TODO: think about logging timestamp? idk if really needed
        public static Action<string> OnInput;

        private static Thread inputThread;
        private static readonly List<string> history = new List<string>();
        private static int historyPosition = -1;
        private static bool isRunning;
        private static readonly object consoleLock = new();

        private static StringBuilder inputBuffer = new();
        private static int cursorPosition = 0;

        public static void Initialize()
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            Logger.LogMessageReceived += OnLogMessageReceived;

            isRunning = true;

            inputThread = new Thread(InputThreadLoop)
            {
                IsBackground = true
            };
            inputThread.Start();
        }

        public static void Destroy()
        {
            Logger.LogMessageReceived -= OnLogMessageReceived;
            isRunning = false;

            inputThread?.Join(500);
        }

        private static void OnLogMessageReceived(LogMessageReceivedEventArgs args)
        {
            lock (consoleLock)
            {
                Console.Write("\r" + new string(' ', Console.WindowWidth - 1) + "\r");

                var color = args.CustomColor ?? args.Type switch
                {
                    LogMessageType.Info => ConsoleColor.Gray,
                    LogMessageType.Debug => ConsoleColor.Yellow,
                    LogMessageType.Warning => ConsoleColor.DarkYellow,
                    LogMessageType.Error => ConsoleColor.Red,
                    LogMessageType.Exception => ConsoleColor.Red,
                    _ => ConsoleColor.Gray
                };

                var oldColor = Console.ForegroundColor;
                Console.ForegroundColor = color;

                if (!string.IsNullOrEmpty(args.Message?.ToString()))
                    Console.WriteLine(args.Message.ToString());

                if (args.Type == LogMessageType.Exception && args.Exception != null)
                    Console.WriteLine(args.Exception.ToString());

                Console.ForegroundColor = oldColor;

                Console.Out.Flush();
                Console.Error.Flush();

                RedrawInputLine();
            }
        }

        private static void InputThreadLoop()
        {
            while (isRunning)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);

                    lock (consoleLock)
                    {
                        switch (key.Key)
                        {
                            case ConsoleKey.Enter:
                                {
                                    string input = inputBuffer.ToString().Trim();

                                    Console.WriteLine();
                                    if (!string.IsNullOrEmpty(input))
                                    {
                                        OnInput?.Invoke(input);
                                        history.Add(input);
                                        if (history.Count > 1000)
                                            history.RemoveAt(0);
                                    }
                                    inputBuffer.Clear();
                                    cursorPosition = 0;
                                    historyPosition = history.Count;
                                    RedrawInputLine();
                                    break;
                                }
                            case ConsoleKey.Backspace:
                                {
                                    if (cursorPosition > 0)
                                    {
                                        inputBuffer.Remove(cursorPosition - 1, 1);
                                        cursorPosition--;
                                        RedrawInputLine();
                                    }
                                    break;
                                }
                            case ConsoleKey.Delete:
                                {
                                    if (cursorPosition < inputBuffer.Length)
                                    {
                                        inputBuffer.Remove(cursorPosition, 1);
                                        RedrawInputLine();
                                    }
                                    break;
                                }
                            case ConsoleKey.LeftArrow:
                                {
                                    if (cursorPosition > 0)
                                    {
                                        cursorPosition--;
                                        SetCursorPos();
                                    }
                                    break;
                                }
                            case ConsoleKey.RightArrow:
                                {
                                    if (cursorPosition < inputBuffer.Length)
                                    {
                                        cursorPosition++;
                                        SetCursorPos();
                                    }
                                    break;
                                }
                            case ConsoleKey.UpArrow:
                                {
                                    if (history.Count > 0 && historyPosition > 0)
                                    {
                                        historyPosition--;
                                        inputBuffer.Clear();
                                        inputBuffer.Append(history[historyPosition]);
                                        cursorPosition = inputBuffer.Length;
                                        RedrawInputLine();
                                    }
                                    break;
                                }
                            case ConsoleKey.DownArrow:
                                {
                                    if (history.Count == 0)
                                        break;

                                    if (historyPosition < history.Count - 1)
                                    {
                                        historyPosition++;
                                        inputBuffer.Clear();
                                        inputBuffer.Append(history[historyPosition]);
                                    }
                                    else
                                    {
                                        historyPosition = history.Count;
                                        inputBuffer.Clear();
                                    }
                                    cursorPosition = inputBuffer.Length;
                                    RedrawInputLine();
                                    break;
                                }
                            default:
                                {
                                    if (!char.IsControl(key.KeyChar))
                                    {
                                        inputBuffer.Insert(cursorPosition, key.KeyChar);
                                        cursorPosition++;
                                        RedrawInputLine();
                                    }
                                    break;
                                }
                        }
                    }
                }
                else
                {
                    Thread.Sleep(50);

                    lock (consoleLock)
                    {
                        RedrawInputLine();
                    }
                }
            }
        }

        private static void RedrawInputLine()
        {
            Console.Write("\r");
            Console.Write(new string(' ', Console.WindowWidth - 1));
            Console.Write("\r> " + inputBuffer.ToString());

            SetCursorPos();
        }

        private static void SetCursorPos()
        {
            int pos = 2 + cursorPosition;
            try
            {
                Console.SetCursorPosition(pos, Console.CursorTop);
            }
            catch
            {
            }
        }
    }
}
