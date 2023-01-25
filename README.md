# qTox RAT
Anonymous Tox Procol Based Remote Administration Tool fully written in c#.

This is a RAT controlled via qTox with over 40 post exploitation modules.

## **Disclaimer:**
This tool is for educational use only, the author will not be held responsible for any misuse of this tool.

## **Setup Guide:**
This code generates a new qTox profile for every unique computer it is ran on.

Then it gets the users friend key and sends a friend request.

The user can then send commands to the qTox profile to control the PC.

**Requirements:**\
Windows(x64)

## **Commands**
```
Here are the list of commands available:
!ping - tests if the connection is working
!whoami - shows the username of the current user
!message hi - shows a message box with the text 'hi'
!privs - shows the privilege level of the program
!uac - elevate uac to gain admin privileges using silent disk cleanup exploit
!cd C:\ - change the working directory of the program
!dir - shows all items in the current directory
!download *file* - downloads the attached file from the computer
!upload *file* - uploads the attached file to the computer
!delete filename - deletes the specified file
!audio *file* - play an audio file on the computer
!voice hi - makes the computer say 'hi' out loud
!wallpaper *image* - changes the computer wallpaper to the attached image
!clipboard - retrieves the computer's clipboard content
!idletime - shows the idle time of the computer
!block - blocks the computer's keyboard and mouse inputs but only if the program is running with admin privileges
!unblock - unblocks the computer's keyboard and mouse inputs but only if the program is running with admin privileges
!screenshot - takes a screenshot of the computer
!webcam - takes a photo of from the webcam
!close - kills the current running process (end the connection)
!uninstall - kills the current running process and deletes all traces of itself from the computer (clean computer)
!shutdown - shut down the computer
!restart - restart the computer
!logoff - log off the current user
!lock - lock the computer
!BSOD - cause a blue screen on the computer but only if the program is running with admin privileges
!plist - lists all running processes
!pkill processname - kills the specified process
!defender = Disable windows defender
!firewall = Disable windows firewall
!task = Disable windows task manager
!crit = Make program a critical process (closed = computer will bluescreen) but only if the program is running with admin privileges
!uncrit = Make program not a critical process but only if the program is running with admin privileges
!website url - open a website on the computer
!startup - create a scheduled task to run the program on startup with admin privileges if possible or as a normal user
!geolocate - get the coordinates of the users IP and the computers built in geolocation
!password - recover passwords from MSedge, Chrome, Firefox, Brave, OperaGX (W.I.P)
!cookie - recover cookies from MSedge, Chrome, Firefox, Brave, OperaGX (W.I.P)
!token - recover discord tokens from discord, canary and web sessions (W.I.P)
!help - list all commands
```
