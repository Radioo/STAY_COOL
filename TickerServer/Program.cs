using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using WebSocketSharp;
using WebSocketSharp.Server;
using Timer = System.Timers.Timer;

namespace TickerServer
{
    public class Program
    {
        public static Dictionary<string, string>? ArgsDic;
        public static Timer? T;
        public static string? HostIP;
        public static int Port = 10573;
        public static IntPtr Address;
        public static string ModuleName;
        public static string Text = string.Empty;
        public static bool Initialized = false;
        public static MemProber Prober;
        public static string FilePath;
        public static IntPtr AutoStart = (IntPtr)0x02900000;
        public static IntPtr AutoEnd = (IntPtr)0x02FFFFFF;

        static void Main(string[] args)
        {
            // Parse arguments
            ArPar parser = new(args);
            ArgsDic = new(parser.ParseArgs());
            Address = new IntPtr(Convert.ToInt64(ArgsDic["-o"], 16));
            ModuleName = ArgsDic["-m"];
            Prober = new();

            // Initialize the timer, check if interval override present 
            T = new Timer(500);
            if (ArgsDic.ContainsKey("-t"))
            {
                string newInterval = ArgsDic["-t"];
                Console.WriteLine($"Setting timer interval to {newInterval}");
                T.Interval = Convert.ToDouble(newInterval);
            }

            // Get local IP address if not specified through parameter
            if (!ArgsDic.ContainsKey("-ip"))
            {
                string localIP = string.Empty;
                using (Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, 0))
                {
                    socket.Connect("8.8.8.8", 65530);
                    IPEndPoint? endPoint = socket.LocalEndPoint as IPEndPoint;
                    HostIP = endPoint.Address.ToString();
                }
            }
            else
            {
                HostIP = ArgsDic["-ip"];
            }

            // Check for port override
            if (ArgsDic.ContainsKey("-port"))
            {
                Port = Convert.ToInt32(ArgsDic["-port"]);
            }

            // Check for file flag
            if (ArgsDic.ContainsKey("-file"))
            {
                // File mode
                Console.WriteLine(@"File mode enabled! Remember to escape the backslashes! (E:\\example.txt)");
                if (ArgsDic.ContainsKey("--auto"))
                {
                    FindAddr();
                }
                FilePath = ArgsDic["-file"];
                Prober.Initialize();
                T.Elapsed += OnTimerElapsed;
                T.Enabled = true;
                Console.ReadLine();
            }
            else
            {
                // Server mode
                WebSocketServer wssv = new WebSocketServer($"ws://{HostIP}:{Port}");
                wssv.AddWebSocketService<Echo>("/Echo");
                wssv.Start();

                if (wssv.IsListening)
                {
                    Console.WriteLine($"Server started at {wssv.Address}:{wssv.Port}, waiting for a connection...");
                }

                Console.ReadLine();
                wssv.Stop();
            }
        }
        static public void OnTimerElapsed(object source, ElapsedEventArgs e)
        {
            string proberesult = Prober.Probe(Address);
            if (Text != proberesult)
            {
                Task.Run(() => File.WriteAllTextAsync(FilePath, proberesult));
                Console.WriteLine($"Wrote: {proberesult}");
                Text = proberesult;
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
            if (ArgsDic.ContainsKey("--auto"))
            {
                if (ArgsDic.ContainsKey("-as"))
                {
                    AutoStart = new IntPtr(Convert.ToInt64(ArgsDic["-as"]));
                }
                if (ArgsDic.ContainsKey("-ae"))
                {
                    AutoEnd = new IntPtr(Convert.ToInt64(ArgsDic["-ae"]));
                }

                Process process = Process.GetProcessesByName(ArgsDic["-p"])[0];
                IntPtr p = MemProber.OpenProcess(0x10, true, process.Id);

                byte[] bytes = { 0x20, 0x20, 0x57, 0x45 };
                byte[] buffer = new byte[bytes.Length];
                IntPtr PTR = AutoStart;
                IntPtr bytesread;

                Console.WriteLine("Looking for target, please stay on the title screen");
                IntPtr endAddr = AutoEnd;
                while (PTR != endAddr)
                {
                    MemProber.ReadProcessMemory(p, PTR, buffer, buffer.Length, out bytesread);
                    if (ByteCompare(buffer, bytes))
                    {
                        Console.WriteLine($"Found the target at address {PTR:X}");
                        PTR -= 0x8;
                        Address = PTR;
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
        static string ProcessName;
        static int BytesToRead = 128;
        IntPtr ModuleAddress;

        public MemProber()
        {
            ProcessName = Program.ArgsDic["-p"];
            // Check for byte number override
            if (Program.ArgsDic.ContainsKey("-b"))
            {
                BytesToRead = Convert.ToInt32(Program.ArgsDic["-b"]);
            }
        }

        public void Initialize()
        {
            Console.WriteLine("Initializing process hook...");
            try
            {
                process = Process.GetProcessesByName(ProcessName)[0];
                processHandle = OpenProcess(PROCESS_WM_READ, false, process.Id);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Oh boy, did you type the process name wrong? Or worse, did you actually not laucnch the game? \n" + ex);
                Console.ReadLine();
                Environment.Exit(3);
            }
            buffer = new byte[Convert.ToInt32(BytesToRead)];

            foreach (ProcessModule m in process.Modules)
            {
                if(m.ModuleName == Program.ModuleName)
                {
                    ModuleAddress = m.BaseAddress;
                    break;
                }
            }
            Console.WriteLine($"Module base address is 0x{ModuleAddress:X}, adding offset 0x{Program.Address:X}");
            Program.Address = IntPtr.Add(ModuleAddress, (int)Program.Address);
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
            return input.Replace("m", ".").Replace("u", ",").Replace("q", "'").Replace('\0', ' ');
        }

        // parses bytes read and stops at the first 0x0 after a string has begun
        public byte[] ParseBytes(byte[] input)
        {
            byte[] output = new byte[Convert.ToInt32(BytesToRead)];
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
        protected override void OnMessage(MessageEventArgs e)
        {
            //string text = prober.Probe(Program.address);
            //Send(text);
            //Console.WriteLine($"Recieved: {e.Data} Sent: {text}");
        }

        // waits for the auto detection if used, starts a timer and assigns the event function
        protected override async void OnOpen()
        {
            Console.WriteLine("Client connected!");
            Send("CONNECTED!");
            if (!Program.Initialized)
            {
                if (Program.ArgsDic.ContainsKey("--auto"))
                {
                    Send("WAITING FOR AUTO DETECTION...");
                }
                bool result = await Task.Run(() => Program.FindAddr());
                if (!result)
                {
                    Send("NO ADDRESS FOUND :( CONNECT AGAIN TO RETRY SCAN");
                }
                else
                {
                    Program.Prober.Initialize();
                    Program.Initialized = true;
                }
            }
            Program.T.Elapsed += OnTimerElapsed;
            Program.T.Enabled = true;
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Console.WriteLine("Client disconnected.");
            Program.T.Elapsed -= OnTimerElapsed;
            Program.T.Enabled = false;
        }

        // sends data to the client if it is different from the previously read one
        public void OnTimerElapsed(object source, ElapsedEventArgs e)
        {
            string proberesult = Program.Prober.Probe(Program.Address);
            if (Program.Text != proberesult)
            {
                Send(proberesult);
                Console.WriteLine($"Sent: {proberesult}");
                Program.Text = proberesult;
            }
        }
    }
}