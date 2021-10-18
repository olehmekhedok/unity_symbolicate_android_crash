using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace SymbolicateAndroidCrash
{
    class Program
    {
        private const string symbolsKey = "symbols";
        private const string callStackKey = "callStack";
        private const string addr2lineKey = "addr2line";

        static void Main(string[] args)
        {
            WriteColor("Create '[config.txt]' file in current directory.", ConsoleColor.Green);

            Console.ReadLine();

            WriteColor("Provide following paths in the file:" + "'[" + symbolsKey + "]','[" + callStackKey + "]','[" + addr2lineKey + "]'", ConsoleColor.Green);
            Console.WriteLine();
            Console.WriteLine("Example:");
            WriteColor("[" + symbolsKey + "]" + @"=C:\symbols-release1\armeabi-v7a\libil2cpp.so", ConsoleColor.Green);
            WriteColor("[" + callStackKey + "]" + @"=C:\callStack.txt", ConsoleColor.Green);
            WriteColor("[" + addr2lineKey + "]" + @"=C:\Program Files\Unity\Hub\...\arm-linux-androideabi-addr2line.exe", ConsoleColor.Green);
            Console.ReadLine();

            while (true)
            {
                if (ReadConfig(out var addr2line, out var symbols, out var callStack))
                {
                    ProcessCallStack(addr2line, symbols, callStack);

                    WriteColor("Do u want to read one more call stack from the file? [Enter 'y' to agree, any other input to quit.]", ConsoleColor.Green);

                    var oneMore = Console.ReadLine();

                    if (oneMore != "y")
                    {
                        return;
                    }

                    Console.WriteLine();
                }
                else
                {
                    WriteColor("[Fix the config and press 'Enter']", ConsoleColor.Red);
                    Console.ReadLine();
                }
            }
        }

        private static void WriteColor(string message, ConsoleColor color)
        {
            var pieces = Regex.Split(message, @"(\[[^\]]*\])");

            for (int i = 0; i < pieces.Length; i++)
            {
                string piece = pieces[i];

                if (piece.StartsWith("[") && piece.EndsWith("]"))
                {
                    Console.ForegroundColor = color;
                    piece = piece.Substring(1, piece.Length - 2);
                }

                Console.Write(piece);
                Console.ResetColor();
            }

            Console.WriteLine();
        }

        private static bool ReadConfig(out string addr2line, out string symbols, out string callStack)
        {
            addr2line = null;
            symbols = null;
            callStack = null;

            try
            {
                var currentDirectory = Directory.GetCurrentDirectory() + @"\config.txt";

                var file = File.ReadAllText(currentDirectory);

                var lines = file.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                foreach (var line in lines)
                {
                    if (line.Contains(symbolsKey))
                    {
                        symbols = line.Split('=')[1];
                        continue;
                    }

                    if (line.Contains(callStackKey))
                    {
                        callStack = line.Split('=')[1];
                        continue;
                    }

                    if (line.Contains(addr2lineKey))
                    {
                        addr2line = line.Split('=')[1];
                        continue;
                    }
                }

                if (symbols == null)
                {
                    WriteColor("[can't find '" + symbolsKey + "' path]", ConsoleColor.Red);
                    return false;
                }

                if (callStack == null)
                {
                    WriteColor("[can't find '" + callStackKey + "' path]", ConsoleColor.Red);
                    return false;
                }

                if (addr2line == null)
                {
                    WriteColor("[can't find '" + addr2lineKey + "' path]", ConsoleColor.Red);
                    return false;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

            return true;
        }

        private static void ProcessCallStack(string addr2line, string symbols, string callStackFile)
        {
            var callStack = File.ReadAllText(callStackFile);

            WriteColor("[Start]:----------------------------------------------", ConsoleColor.Green);
            Console.WriteLine();

            var lines = callStack.Split(
                new string[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            foreach (var line in lines)
            {
                var split = line.Split('.');
                var hash = split[1];
                Console.WriteLine(hash);
                var log = Symbolicate(addr2line, "-f -C -e " + symbols + " " + hash);

                Console.WriteLine(log);
            }

            WriteColor("[End]:----------------------------------------------", ConsoleColor.Green);

            Console.WriteLine();
        }

        private static string Symbolicate(string path, string cmd)
        {
            var procStartInfo = new ProcessStartInfo(path, cmd);

            procStartInfo.RedirectStandardOutput = true;
            procStartInfo.UseShellExecute = false;
            procStartInfo.CreateNoWindow = true;

            string result;

            using (Process process = new Process())
            {
                process.StartInfo = procStartInfo;
                process.Start();

                process.WaitForExit();

                result = process.StandardOutput.ReadToEnd();
            }

            return result;
        }
    }
}
