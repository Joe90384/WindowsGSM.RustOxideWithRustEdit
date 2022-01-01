# WindowsGSM RustOxideWithRustEdit Plugin
![Logo](https://github.com/Joe90384/WindowsGSM.RustOxideWithRustEdit/blob/main/RustOxideWithRustEdit.cs/RustOxideWithRustEdit.png?raw=true)

##What can it do
This is a plugin for [WindowsGSM](https://windowsgsm.com/) which allows you to:
* Create a new Rust server [Oxide](https://umod.org/) and [RustEdit](https://www.rustedit.io/)
* Auto update Oxide and RustEdit to the latest version
* Schedule automatic wipes of maps, blueprints

##Getting started
1. Download this project as a zip file
2. Click the <span style='background:---; border:1 1 solid #000'><img src="https://raw.githubusercontent.com/WindowsGSM/WindowsGSM/master/WindowsGSM/Images/HMenu/ViewPlugins.ico" width=25/></span> button in Windows GSM
3. Click "Import Plugin" and select the zip file you downloaded
4. RustOxideWithRustEdit should have been added to your "Loaded Plugin List"

##Creating a new server
1. Click the "Servers" menu at the top of the window
2. Click "<img src="https://raw.githubusercontent.com/WindowsGSM/WindowsGSM/master/WindowsGSM/Images/Install.ico" width=25/> <b>Install Game Server</b>" from the menu
3. Click the Game Server dropdown and select "Rust Dedicated Server with Oxide and RustEdit [RustOxideWithRustEdit.cs]"
4. Set your server name
5. Click "Install"
6. You server should now be created and will also install the latest version of Oxide and RustEdit

##Migrating an existing "Rust Dedicated Server" that was installed with WindowsGSM
1. Identify the ID of the server you wish to migrate
2. Open the WindowsGSM folder in windows explorer
3. Navigate to servers/[serverid]/configs
4. Open the WindowsGSM.cfg file in an editor
5. Change the first line from:
```servergame="Rust Dedicated Server``` to ```servergame="Rust Dedicated Server with Oxide and RustEdit [RustOxideWithRustEdit.cs]"```
6. Save the file and restart WindowsGSM
7. Select the server and click the "Update" button
8. This should install the latest versions of Oxide and RustEdit onto the server

##Configuring the wipe schedule
Wipes will occur at the next restart after a scheduled wipe. For this to work the server must be start by WindowsGSM

1. Identify the ID of the server you wish to set a wipe schedule on
2. Open the WindowsGSM folder in windows explorer
3. Navigate to servers/[serverid]/configs
4. If you did a clean install you should find a file called auto_wipe.template.cfg
5. Rename this to auto_wipe.cfg
6. Adjust the setting to match your requirements

###Wipe Schedule options
The wipe schedule is a json file with each schedule being an json object.
```json
  "[Schedule Name]": { 
    "weeks": 
    "day": 
    "time": 
    "files" 
  }
```
Each schedule has:
* A primary key which is the [Schedule Name] this must be unique for each schedule
* A "weeks" property
  * This indicates which weeks of the month the schedule should run on
    * `"weeks": [1,2,3,4,5] ` would wipe on every week in the month
    * `"weeks": [1]` would only wipe on the 1st week in the month
* A "day" property
  * This indicates which day of the week the schedule should run on
    * 0 = Sunday, 1 = Monday, 2 = Tuesday, 3 = Wednesday, 4 = Thursday, 5 = Friday, 6 = Saturday 
    * `"day": 4` would wipe on a Thursday
* A "time" property
  * This indicates what time the wipe should occur.
    * `"time": "12:00"` The wipe will occur at 12:00 (midday server time)
* A "files" property
  * This indicates which files should be deleted from the server instance folder. This allows for wildcard pattern matching
    * `"files": ["*.sav"]` Will delete all files with the .sav extension
    * `"files": ["player.blueprints.5.db"]` will delete the player blueprints file only
    * `"files": ["*.*"]` Will delete all files in the server instance folder

Once the server has run up at least once you will see an additional property added:
* A "next" property
  * This indicates the next schedule wipes date and time.


###auto_wipe.template.cfg
```json
{
  "Map Wipe": {                                       // Name for this wipe schedule (must be unique)
    "weeks": [2,4],                                   // Wipe on 2nd and 4th weeks
    "day": 4,                                         // Wipe on Thursday (0 - 6 => Sunday - Saturday)
    "time": "12:00",                                  // Wipe at next restart after 12:00 (midday server time)
    "files": ["*.sav"]                                // Wipe map only
  },
  "BP Wipe": {                                        // Name for this wipe schedule (must be unique)
    "weeks": [0,2,4],                                 // Wipe on 1st, 3rd and 5th weeks
    "day": 4,                                         // Wipe on Thursday
    "time": "18:29",                                  // Wipe at next restart after 12:00 (midday server time)
    "files": ["*.db","*.sav","*.db-journal"]          // Wipe map and blueprints
  }
}
```
