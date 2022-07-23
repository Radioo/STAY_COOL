![Preview](https://stn.s-ul.eu/A8f49Dzl.png)

# Deprecated
New and better hook dll can be found here: https://github.com/Radioo/TickerHook

# STAY_COOL
HTML ticker display for IIDX

# Usage
STAY_COOL comes in two parts: server and client. The server is responsible for reading the game's memory, parsing it and sending it to the client. On the other hand, the client receives the data and displays it on an HTML page.

**Server setup**  
The server needs to be ran with admin priviliges!
You need to provide additional arguments as well, they are listed below:

Example: `TickerServer.exe -p spice64 -m bm2dx.dll -o 66C7270 -file D:\\test.txt`  

`-o 0x10C7C608` REQUIRED - Offset from base module address which points to the ticker.  
`-p launcher` REQUIRED - Game's process name.  
`-m bm2dx.dll` REQUIRED - Name of the module which holds the ticker address.  
`-t 500` - Memory scan frequency in ms (default 500).  
`-b 128` - Ticker text array size (default 128).  
`-ip 10.0.0.12` - Local IP address override (use if auto detection fails).  
`-port 10573` - Port override (default 10573).  
`-file E:\\test.txt` - File mode (prints text to the specified file, overriding it each time. Remember to escape the backslashes!).  
`--auto` - Automatic memory lookup.  
`-as 0x02900000` - Starting address for `--auto` (default 0x02900000).  
`-ae 0x02FFFFFF` - Ending address for `--auto` (default 0x02FFFFFF).  

Easiest way to automatically launch it is to add it to your gamestart.bat like so:  
`start "Ticker server" "C:\Users\Radio\Documents\Builds\TickerServer\TickerServer.exe" -p spice64 -m bm2dx.dll -o 66C7270 -file D:\\test.txt`  

**Automatic memory lookup**  
By using `--auto`, you can enable automatic memory lookup. This is only needed if the ticker address changes every boot, for example: Toastertools running IIDX 18 or 19.  
By default it begins searching from `0x02900000` and stops at `0x02FFFFFF`. It looks for an ASCII string composed of two spaces and characters `WE`. This is important because you need the game to stay on the title screen (starting on the warning screen and through the title screen music, demo play screen changes the text to `DEMO PLAY`) through the duration of the scan. The client text will inform you if the scan didn't find anything, in that case you will need to restart the game or change the start and end addresses for lookup. I wish I could've handled it better but I'm still new to this, sorry...

**Client setup**  
Set the IP address of your server in the client.js file. Example: `let socket = new WebSocket("ws://10.0.0.31:10573/Echo");`  
Only change the address, so `10.0.0.31` in this case. Then just run `index.html`.

It's important to understand the connection behavior. As soon as you load the page, the client will attempt a connection to the server. If successful, the server will begin to initialize the process hook. Just remember to connect after the server has a process to hook into.  
**If using `--auto`: Remember to start connecting only when the game is on the title screen as explained in the auto section**

**Customizing the look**  
You can customize to your heart's content using style.css.

# Offsets  

09 0x14EF7E4  
10 0x154553C  
11 0x117B474  
12 0x1524F28  
13 0x1BF8A30  
14 0x25427A0  
15 0x25F2460  
16 0x257E820  
17 0x1F99BF0  
18 0x461978  
19 0x542408  
20 0xC7C608  
21 0x141ACE8  
22 0x143AC28  
23 0x1421F58  
24 0x16A9B68  
25 0x24AB390  
26 0x313DAF0  
27 0x47C5BB0  
28 0x554B3E0  
29 (2021-11-17) 0x66C7270  
30 0x0x858BA62BC5  
