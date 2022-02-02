using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Timers;
using Timer = System.Timers.Timer;

namespace TickerServer
{
    public class Program
    {
        public static IntPtr address;
        public static IntPtr startaddr = (IntPtr)0x02A00000;
        public static IntPtr endaddr = (IntPtr)0x02FFFFFF;
        public static string ProcessName;
        public static string HostAddress;
        public static int bytes;
        public static bool auto = false;
        public static Timer t;
        public static string text = "";

        static void Main(string[] args)
        {
            // check for invalid usage
            if (args.Length < 5)
            {
                Console.WriteLine("Usage: TickerServer.exe MemoryLocation ProcessName HostAddress NumberOfBytesToRead ScanInterval AutoFlag(optional) AutoStartAddr(optional) AutoEndAddr(optional) \n" +
                    "Example: Tickerserver.exe 0x10C7C814 launcher 10.0.0.31 128 500");
                Console.ReadLine();
            }
            else
            {
                // check if provided address is 32-bit or 64-bit
                if (args[0].Length <= 10)
                {
                    address = new IntPtr(Convert.ToInt32(args[0], 16));
                }
                else
                {
                    address = new IntPtr(Convert.ToInt64(args[0], 16));
                }
                /*
                if (args[0] == "32")
                {
                    address = new IntPtr(Convert.ToInt32(args[1], 16));


                }
                else if (args[0] == "64")
                {
                    address = new IntPtr(Convert.ToInt64(args[1], 16));
                }
                */

                // parse args
                ProcessName = args[1];
                HostAddress = args[2];
                bytes = Convert.ToInt32(args[3]);

                t = new();
                t.Interval = Convert.ToDouble(args[4]);

                // start server
                WebSocketServer wssv = new WebSocketServer($"ws://{HostAddress}:10573");

                wssv.AddWebSocketService<Echo>("/Echo");

                wssv.Start();
                Console.WriteLine("Server started");

                // check for auto flag
                if (args.Length == 6 && args[5] == "auto")
                {
                    auto = true;
                }
                else if (args.Length == 8)
                {
                    if (args[6].Length <= 10)
                    {
                        startaddr = new IntPtr(Convert.ToInt32(args[6], 16));
                    }
                    else
                    {
                        startaddr = new IntPtr(Convert.ToInt64(args[6], 16));
                    }
                    if (args[7].Length <= 10)
                    {
                        endaddr = new IntPtr(Convert.ToInt32(args[7], 16));
                    }
                    else
                    {
                        endaddr = new IntPtr(Convert.ToInt64(args[7], 16));
                    }
                    auto = true;
                }

                // stop the server with the enter key
                Console.ReadLine();
                wssv.Stop();
            }
        }

        // compare function, probably slow, sorry
        public static bool ByteCompare(byte[] source, byte[] input)
        {
            int srclen = source.Length;
            int inplen = input.Length;
            if (srclen != inplen)
            {
                return false;
            }
            for (int i = 0; i < inplen; i++)
            {
                if (source[i] != input[i])
                {
                    return false;
                }
            }
            return true;
        }

        // this finds the address with the auto mode, messy, not good
        public static bool FindAddr()
        {
            if (auto)
            {
                Process process = Process.GetProcessesByName("launcher")[0];
                IntPtr p = MemProber.OpenProcess(0x10, true, process.Id);

                byte[] bytes = { 0x20, 0x20, 0x57, 0x45 };
                byte[] buffer = new byte[bytes.Length];
                IntPtr PTR = startaddr;
                IntPtr bytesread;

                Console.WriteLine("Looking for target, please stay on the title screen");
                while (PTR != endaddr)
                {
                    MemProber.ReadProcessMemory(p, PTR, buffer, buffer.Length, out bytesread);
                    if (ByteCompare(buffer, bytes))
                    {
                        Console.WriteLine($"Found the target at address {PTR:X}");
                        PTR -= 0x8;
                        address = PTR;
                        Console.WriteLine(Encoding.ASCII.GetString(buffer));
                        Console.WriteLine("Scan finished.");
                        return true;
                    }
                    PTR += 0x1;
                }
                Console.WriteLine("Scan finished, no address found :( \n" +
                    "Try restarting or changing search bounds");
                return false;
            }
            else return true;
        }
    }

    // memory prober class
    public class MemProber
    {
        public static Process process;
        IntPtr processHandle;
        byte[] buffer;

        public MemProber()
        {

        }

        public void Initialize()
        {
            Console.WriteLine("Initializing process hook...");
            process = Process.GetProcessesByName(Program.ProcessName)[0];
            processHandle = OpenProcess(PROCESS_WM_READ, false, process.Id);
            buffer = new byte[Program.bytes];
            Console.WriteLine("Done!");
        }

        public string Probe(IntPtr address)
        {
            IntPtr bytesRead;
            // tricoro 0x10C7C814 bistrover 0x18554B5EC
            ReadProcessMemory(processHandle, address, buffer, buffer.Length, out bytesRead);
            string res = ParseString(Encoding.UTF8.GetString(ParseBytes(buffer))).Trim();
            // Console.WriteLine($"Out: {res.Trim()}");
            return res;
        }

        // convert ticker specific codes for dots and the like
        public string ParseString(string input)
        {
            return input.Replace("m", ".").Replace("u", ",").Replace("q", "'");
        }

        // parses bytes read and stops at the first 0x0 after a string has begun
        public byte[] ParseBytes(byte[] input)
        {
            byte[] output = new byte[Program.bytes];
            int count = 0;
            bool firstchar = false;

            foreach (byte b in input)
            {
                if (b != 0)
                {
                    output[count++] = b;
                    firstchar = true;
                }
                else if (b == 0 && firstchar)
                {
                    break;
                }
            }
            return output;
        }

        const int PROCESS_WM_READ = 0x0010;

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess,
            IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);
    }

    public class Echo : WebSocketBehavior
    {
        MemProber prober;

        protected override void OnMessage(MessageEventArgs e)
        {
            //string text = prober.Probe(Program.address);
            //Send(text);
            //Console.WriteLine($"Recieved: {e.Data} Sent: {text}");
        }

        // waits for the auto detection if used, starts a timer and assigns the event function
        protected override async void OnOpen()
        {
            if (Program.auto)
            {
                Send("WAITING FOR AUTO DETECTION...");
            }
            bool result = await Task.Run(() => Program.FindAddr());
            if (!result)
            {
                Send("NO ADDRESS FOUND :(");
            }
            else
            {
                prober = new();
                prober.Initialize();

                Program.t.Elapsed += OnTimerElapsed;
                Program.t.Enabled = true;
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Program.t.Enabled = false;
            Program.t.Elapsed -= OnTimerElapsed;
        }

        // sends data to the client if it is different from the previously read one
        public void OnTimerElapsed(object source, ElapsedEventArgs e)
        {
            string proberesult = prober.Probe(Program.address);
            if (Program.text != proberesult)
            {
                Send(proberesult);
                Console.WriteLine($"Sent: {proberesult}");
                Program.text = proberesult;
            }
        }
    }
}