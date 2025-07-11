using Pyratron.Frameworks.Commands.Parser;
using Server;
using System;

namespace GettingOverItMP.Server.ConsoleCommands
{
    [Command("Clear", "clear", "Clear the console.")]
    public class ClearCommand : ConsoleCommand
    {
        public override void Handle(string[] args)
        {
            for (int i = 0; i < Console.WindowHeight; i++)
                Console.WriteLine();
        }
    }
}
