# FikOff
A small program that helps you switch between FIKA's coop and standard SPT's single player modes.
Not to be confused with *FuckOff*.

## FikOff's functionality
### FikOff will:
* Read from `lang.json` to determine how to piss you off in your preferred language.
* Read command-line arguments if provided
* "Install" FIKA provided you have the files hanging around.
* "Uninstall" FIKA (just the .dll and mod folder, not your profiles or dignity).
* Detect if you have an `ip.txt` file and act accordingly.
* Configure your Launcher to connect to the correct IP depending on your chosen mode.

### FikOff will NOT:
* Enable developer mode in the SPT Launcher for you.
* Download FIKA files.
* Automatically set up FIKA if you’ve never touched it.
* Make you a cup of coffee in the morning, say "I love you baby" when you wake up, and hug you when you feel sad...

## Mode switiching
### When switching to multiplayer:
* FikOff checks for `ip.txt`. If missing, warns you and falls back to SP mode.
* If found, it deletes any old FIKA files.
* It then copies the needed `Fika.Core.dll` and `fika-server` folder from `_fika` into the proper plugin and mod directories.
* After which, the script will access your launcher's `config.json` file and change your set IP address to the one inside `ip.txt` that you hopefully didn't forget to set up.
* If multiple MP IPs are detected in the file, FikOff will let you choose which one to use.

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
  * Use the **first line** as the IP for singleplayer
  * Use the **second line** as the IP for multiplayer
  * Offer you to pick between one of the other lines for the MP IP (if there are more than 2).

If `ip.txt` is missing, it defaults to the standard SPT-AKI localhost IP: `https://127.0.0.1:6969`.
**ip.txt structure should be as follows:**
```
https://127.0.0.1:6969
https://420.42.69.4:6969
...other IPs if you're that much into server hopping
```
One IP per line and nothing more.
## Folder Structure
Your SPT install should look like this, apart from the standard SPT files:

```
SPT Root/
├── FikOffCS.exe
├── FikOff.py
├── lang.json
├── ip.txt
├── _fika/
│   ├── BepInEx/plugins/Fika.Core.dll
│   └── user/mods/fika-server/
```

## Requirements
* [.NET Desktop Runtime 8.0 (x64)](https://dotnet.microsoft.com/pt-br/download/dotnet) or later (for `FikOffCS.exe`)
* [Python 3.6](https://www.python.org/downloads/) or later (for `FikOff.py`)
* SPT Launcher's developer mode should be turned **ON** for FikOff to succesfully change the IP address.
* FIKA files must be extracted to `_fika/` as shown above
* `ip.txt` must exist for multiplayer to work. Without it, you get SP only.

## Installation
[Download one of the releases](https://github.com/jesga06/FikOff/releases) and drag the following into your SPT root folder:
- `FikOffCS.exe` or `FikOff.py`
- `lang.json`
- `ip.txt`
## Command-line args:
You can skip the prompts by using these args:

### `--launchmode`
Starts the app in the given mode:

- `sp` → Singleplayer
- `mp` → Multiplayer
- `quick` → Instantly starts SP using localhost IP

### `--setup`
Triggers install/uninstall of FIKA files based on `--launchmode`  
- Optional (default = false)

### `--ip-index`
Uses the IP at the given (1-indexed) index from `ip.txt` for MP  
- If missing and you're in MP mode, FikOff will ask for it

### `--dry-run`
Runs the logic without actually doing anything  
- For debugging only

### `--log`
Enables logging using internal `log()` calls
### Examples:
```
FikOffCS.exe --launchmode mp --ip-index 3
FikOff.py --launchmode sp --setup --ip-index 1
FikOff.py --launchmode quick
```
# Troubleshooting
❓ **FikOff doesn't work**  
- Check if your paths are correct  
- Make sure your antivirus didn’t block it  

❓ **IP doesn't update in the launcher**  
- Enable **developer mode** in the SPT Launcher  

❓ FikOff is telling me to, well, \"Fuck Off"\: 
- Congratulations. You probably mistyped a basic CLI option.

# Disclaimers
FikOff is a ~~steaming hot pile of shit~~ chaotic little helper. It’s not perfect, but it gets the job done and rarely explodes.

If you want to contribute in any way - be it via localizations or coding, be my guest and help a 2nd semester CS student out.

### How to contribute:
* Fork this repo
* Do your thing
* Open a pull request with a short description

When adding new localizations, follow JSON formatting rules and **don’t remove `\n` line breaks** unless you want your terminal to look like your PMC after being hit by Tagilla's sledgehammer.

### About AI usage:
The only times were AI was used in this project were to:
* Refactor hardcoded strings into language-based lookups (because I am a moron and forgot to do that);
* Give me a draft markdown file that eventually turned into this mess of a 'readme';
* Translate all of this shit from python to a language that can be compiled into something that *should* take no more than a double click to start.
* Translate strings to languages other than English and *True Portuguese*™
  * PS: You're still welcome to submit translation PRs — just note what’s broken or missing.

### To-do:
* ~~Add command-line arguments so playnite users such as myself can just start their game without having to see an ugly terminal~~ Done.

### Fun fact
The script may roast or make fun of you. Especially if you mess up your input. That's just tough love. Enjoy.

## License
This project is licensed under the [MIT License](LICENSE).
