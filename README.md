# GetLocalAdmins
[@secure_sean](https://twitter.com/secure_sean)

April 2022

Typical Windows 10 Home:
![HighlightedTextOutput](Asssets/screenshot1.jpg)


Who can remotely access this Windows computer? This program prints information about users from the various remote access groups in Windows in a messy (hardcoded) console table. Default is local machine, arguments support remote machines on the same domain (auth'd user context required but not working right now)

## Features
- Prints information about your user
- Prints local group membership information
- HighLights your user when printing your group membership
- Logs all output to file `Output.log`