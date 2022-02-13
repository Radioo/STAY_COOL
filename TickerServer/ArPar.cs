using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TickerServer
{
    class ArPar
    {
        public int ArgCount { get; private set; }
        private readonly string[] Args;

        public ArPar(string[] args)
        {
            Args = args;
            ArgCount = args.Length;
        }
        public Dictionary<string, string> ParseArgs()
        {
            Dictionary<string, string> result = new();

            for (int i = 0; i < ArgCount; i++)
            {
                if (Args[i].StartsWith("-"))
                {
                    if (Args[i].StartsWith("--"))
                    {
                        Console.WriteLine($"Set {Args[i]} = 1");
                        result.Add(Args[i], "1");
                    }
                    else
                    {
                        try
                        {
                            Console.WriteLine($"Set {Args[i]} = {Args[i + 1]}");
                            result.Add(Args[i], Args[i + 1]);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Let me guess, you tried to pass a parameter at the end but forgot to give it a value? It's OK, we all make mistakes. Anyways here's the exception for you to stare at and be confused:\n" + ex);
                            Console.ReadLine();
                            Environment.Exit(2);
                        }
                    }
                }
            }
            return result;
        }
    }
}
