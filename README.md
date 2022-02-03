![Preview](https://stn.s-ul.eu/A8f49Dzl.png)

# STAY_COOL
HTML ticker display for IIDX

# Usage
STAY_COOL comes in two parts: server and client. The server is responsible for reading the game's memory, paring it and sending it to the client. On the other hand, the client receives the data and displays it on an HTML page.

**Server setup**  
The server needs to be ran with admin priviliges!
You need to provide additional arguments as well, they are listed below:

Example: `TickerServer.exe 0x10C7C608 launcher 10.0.0.31 128 500`

1: The game's ticker memory address. This will vary from game to game and maybe even between different tools. As long as it stays the same on each boot, you're good to go. I've included the addresses I found while testing at the bottom.  
2: Game's process name. `bm2dx` for 9-17, `launcher` for 18+, `spice` or `spice64` if using spicetools.  
3: Local IP address of the host.  
4: Number of bytes to read. You can make it lower if you want, I ran it with 64 for a long time. If your ticker text is too long and gets cut off, increase it. I found 128 works quite well.  
5: Memory reading interval in ms. Less means faster reads but may cost performance. Actually I don't know if it's that big of a deal with today's hardware. I found 500 works quite well.  
6(optional): Automatic memory lookup, explained below.  
7(optional): Start address for auto lookup. Default: `0x02A00000`  
8(optional): End address for auto lookup. Default: `0x02FFFFFF`  

Easiest way to automatically launch it is to add it to your gamestart.bat like so:  
`start "Ticker server" "C:\Users\Radio\Documents\Builds\TickerServer\TickerServer.exe" 0x10C7C608 launcher 10.0.0.31 128 500`  

**Automatic memory lookup**  
By adding `auto` as the 6th argument, you can enable automatic memory lookup. This is only needed if the ticker address changes every boot, for example: Toastertools running IIDX 18 or 19.  
By default it begins searching from `0x02A00000` and stops at `0x02FFFFFF`. It looks for an ASCII string composed of two spaces and characters `WE`. This is important because you need the game to stay on the title screen (starting on the warning screen and through the title screen music, demo play screen changes the text to `DEMO PLAY`) through the duration of the scan. The client text will inform you if the scan didn't find anything, in that case you will need to restart the game or change the start and end addresses for lookup. I wish I could've handled it better but I'm still new to this, sorry...

**Client setup**  
Set the IP address of your server in the client.js file. Example: `let socket = new WebSocket("ws://10.0.0.31:10573/Echo");`  
Only change the address, so `10.0.0.31` in this case. Then just run `index.html`.

It's important to understand the connection behavior. As soon as you load the page, the client will attempt a connection to the server. If successful, the server will begin to initialize the process hook. Just remember to connect after the server has a process to hook into.  
**If using `auto`: Remember to start connecting only when the game is on the title screen as explained in the auto section**

**Customizing the look**  
You can customize to your heart's content using style.css.

# Addresses  
All of these were found while running btools, might work on spice as well, haven't tested.

09 0x18EF7E4  
10 0x0194553C  
11 0x0157B474  
12 0x01924F28  
13 0x01FF8A30  
14 0x029427A0  
15 0x029F2460  
16 0x0297E820  
17 0x02399BF0  
18 0x0 (use auto, toastertools makes the address change on each boot)  
19 0x0 (same as 18)  
20 0x10C7C608  
21 0x1141ACE8  
22 0x1143AC28  
23 0x11421F58  
24 0x116A9B68  
25 0x1824AB390  
26 0x18313DAF0  
27 0x18554B3E0  
28 0x18554B3E0   
29 0x1866C7270
30 0x858BA62BC5
