# FikOff
A small program that helps you switch between FIKA's coop and standard SPT's single player modes.

## FikOff's functionality
### FikOff will:
* Read from `lang.json` to determine how to piss you off in your preferred language.
* "Install" FIKA provided you have the files hanging around.
* "Uninstall" FIKA provided you or the app itself didn't fuck up the install.
* Detect if you have an `ip.txt` file and act accordingly.
* Configure your Launcher to connect to the correct IP depending on your chosen mode.

### FikOff will not:
* Turn on developer mode in the SPT Launcher
* Automatically and correctly setup FIKA if you haven't already.
* Download necessary FIKA files for you.
* Make you a cup of coffee in the morning, say "I love you baby" when you wake up, and hug you when you feel sad...

## Mode switiching
### When switching to multiplayer:
* FikOff tries to uninstall any existing FIKA traces (not your data/profiles, just the .dll and the server mod) to avoid possible errors.
* It then copies the needed `Fika.Core.dll` and `fika-server` folder from `_fika` into the proper plugin and mod directories.
* After which, the script will access your launcher's `config.json` file and change your set IP address to the one inside `ip.txt` that you hopefully didn't forget to set up.
* If multiple MP IPs are detected in the file, FikOff will ask you to choose which one to use.

### When switching to singleplayer:
* FikOff attempts to delete both the `Fika.Core.dll` plugin and the `fika-server` folder.
* Then it edits the `config.json` used by the SPT launcher, swapping the IP back to the singleplayer one.

### Launcher behavior
* After setting everything up, FikOff will ask if you want to automatically start the launcher (and the server, if in SP mode).
* If you say yes:
  * SP mode: it starts the server, waits 10 seconds (because FikOff knows that you have a hoarding issue regarding server mods), then starts the launcher.
  * MP mode: only the launcher is opened.
* If you say no, it just finishes and lets you launch manually.

### In case you input anything invalid:
* You will get flipped off.

## About the "ip.txt" file:
### FikOff will:
As mentioned previously:
  * Use the **first line** as the IP for singleplayer and the **second line** as the IP for multiplayer
  * Iterate through the other lines (if there are more than 2) and allow you to choose which IP you want to connect to.

If `ip.txt` is missing, it defaults to the standard SPT-AKI localhost IP (`https://127.0.0.1:6969`).
**The file structure should be as follwows:**
```
https://127.0.0.1:6969
https://420.42.69.4:6969
...other IPs if you're into server hopping that much
```
One IP per line and nothing more.

## Requirements

* SPT Launcher's developer mode should be turned **ON** for FikOff to succesfully change the IP address.
* Folder structure must be compatible (you need a `_fika` backup folder with the correct mod/plugin structure).
  * The folder above must have the following structure:
    * `/_fika/BepInEx/plugins/Fika.Core.dll`
    * `/_fika/user/mods/fika-server`
    * basically, just extract the necessary FIKA files to `_fika` and call it a day
* `ip.txt` must exist for multiplayer to work. Without it, you get SP only.
* Your SPT install must use `SPT.Server.exe`, `SPT.Launcher.exe`, and the typical AKI layout.

## Installation
literally just drop `fikoff.py` or `fikoff.exe` into your spt folder and make a shortcut for it

## Disclaimers
FikOff is a steaming hot pile of shit and not a good piece of code *at all*, but it works nice enough and doesn't break.

If you want to contribute in any way - be it via localizations or coding, be my guest.

### About AI usage:
The only times were AI was used in this project were to:
* Change all of the hardcoded language-specific prints to ones that fetch the correct string from the `lang.json` file (because I am a moron and forgot to do that);
* Give me a template markdown file that eventually turned into this mess of a 'readme';
* Translate all of this shit to a language that can be compiled into something that takes no more than a double click to start.

## Fun fact
The script may make fun of you or curse at you. Especially if you mess up your input. That's just tough love. Enjoy.
