using System;

namespace AlpVM_Assembler
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: {0} command outfile infile(s).", "builder");
                Console.WriteLine("command: a for assembling l for linking");
                return;
            }
            else if (args.Length == 1)
            {
                Console.WriteLine("no output file");
                return;
            }
            else if (args.Length == 2)
            {
                Console.WriteLine("no input file");
                return;
            }
            else
            {
                try
                {
                    string command = args[0];
                    string outFile = args[1];

                    if (command == "a")
                    {
                        Console.WriteLine("AlpVM Assembler");
                        if (args.Length > 3)
                        {
                            Console.WriteLine("too many input files. assembler needs one input file and one output file.");
                            return;
                        }
                        else
                        {
                            string inFile = args[2];

                            Assembler assembler = new Assembler();

                            assembler.Assemble(inFile);

                            assembler.SaveBinary(outFile);
                        }
                    }
                    else if (command == "l")
                    {
                        Console.WriteLine("AlpVM Linker");

                        Linker.Linker linker = new Linker.Linker();

                        for (int i = 2; i < args.Length; ++i)
                        {
                            linker.AddInputFile(args[i]);
                        }

                        linker.Link();
                        linker.SaveBinary(outFile);
                    }
                    else
                    {
                        Console.WriteLine("unknown command: {0}", command);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: {0}", ex.Message);
                }
            }
        }
    }
}
