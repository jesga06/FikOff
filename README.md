# FikOff

A small multi-language* program that helps you switch between FIKA's coop and standard SPT's single player modes.

Not to be confused with *FuckOff*

*ported from Python to other langs

## FikOff's functionality

### FikOff will

  * Turn on developer mode in the Launcher automatically.
  * Read from `lang.json` to determine how to piss you off in your preferred language.
  * Read command-line arguments if you provided them.
  * "Install" FIKA provided you have the files hanging around.
  * "Uninstall" FIKA provided you or the app itself didn't fuck up the install.
    * Will only uninstall the plugin and the server mod. Your profiles and configurations are safe.
  * Detect if you have an `ip.txt` file and act accordingly.
  * Configure your Launcher to connect to the correct IP depending on your chosen mode.

### FikOff will not

  * Automatically and correctly setup FIKA if you haven't already.
  * Download necessary FIKA files for you.
  * Make you a cup of coffee in the morning, say "I love you baby" when you wake up, and hug you when you feel sad...

## Mode switching

### When switching to multiplayer

  * FikOff will search for the `ip.txt` file, will warn you if it doesn't exist and will automatically fall back to SP
  * If everything goes right, it'll try to uninstall any existing FIKA traces to avoid possible errors.
  * It then copies the needed `Fika.Core.dll` and `fika-server` folder from `_fika` into the proper plugin and mod directories.
  * After which, the script will access your launcher's `config.json` file and change your set IP address to the one inside `ip.txt` that you hopefully didn't forget to set up.
  * If multiple MP IPs are detected in the file, FikOff will ask you to choose which one to use.

### When switching to singleplayer

  * FikOff attempts to delete both the `Fika.Core.dll` plugin and the `fika-server` folder.
  * Then it edits the `config.json` used by the SPT launcher, swapping the IP back to the singleplayer one.

### Launcher behavior

**Interactive Mode:**

  * After the setup, FikOff will ask if you want to automatically start the relevant processes according to your chosen mode (`Y/N`).
  * If you say `Y`:
      * **SP & MPH modes:** It starts the server, actively waits for it to come online (checking every 7 seconds for up to 420 seconds), and then starts the launcher.
        * FikOff knows you have a hoarding issue regarding server mods. That's why.
      * **MPC mode:** It only starts the launcher, since the server is hosted by someone else.
  * If you say `N`, the script finishes and you can launch everything manually if your heart desires.

**CLAP Mode (using `--launchmode`):** - **C**ommand-**L**ine **A**uto **P**ilot Mode

  * The script will **always** attempt to start the processes automatically according to the selected mode (`sp`, `mph`, or `mpc`). The interactive `Y/N` prompt is skipped. The behavior for each mode is the same as described above.

### In case you input anything invalid

  * You will be told to *FikOff™*.

## About the "ip.txt" file

FikOff will:

  * Use the **first line** as the IP for singleplayer.
  * The **second and all subsequent lines** are treated as possible IPs for multiplayer.
  * If only one multiplayer IP is found (i.e., the file has only two lines), it will be selected automatically. If multiple multiplayer IPs are found, FikOff will ask you to choose which one to use **in both** interactive and CLAP modes.

If `ip.txt` is missing, it defaults to standard localhost IP (`https://127.0.0.1:6969`).

**The file structure should be as follows:**

```
https://127.0.0.1:6969
https://420.42.69.4:6969
...other IPs if you're that much into server hopping
```

One IP per line and nothing more.

## Requirements
  * [.NET Desktop Runtime 8.0 (x64)](https://dotnet.microsoft.com/download/dotnet) or later (for `FikOffCS.exe`)
  * [Python 3.6](https://www.python.org/downloads/) or later (for `FikOff.py`)
  * Folder structure must be compatible (you need a `_fika` backup folder with the correct mod/plugin structure).
      * The folder above must have the following structure:
      * `/_fika/BepInEx/plugins/Fika.Core.dll`
      * `/_fika/user/mods/fika-server`
      * basically, just extract the necessary FIKA files to `_fika` and call it a day
  * `ip.txt` must exist for multiplayer to work. Without it, you get SP only.
  * Your SPT install must use `SPT.Server.exe`, `SPT.Launcher.exe`, and the typical AKI layout.

## Installation

[Download one of the releases](https://github.com/jesga06/FikOff/releases) and drag the following into your SPT root folder:
- `FikOffCS.exe`, `FikOff.py`, or `FikOff.bat`
- `lang.json`
- `ip.txt`
Set up a shortcut if you want to use CLAP without using Steam, Playnite, or other game managers.

## Command-line args

| Argument (.py) | Equivalent (.bat) | Description |
| :--- | :--- | :--- |
| `--launchmode` | `/mode` | Will launch the app in CLAP mode. Automatically sets up IPs if `--quick` isn't provided.|
| `--setup` | `/setup` | Will install and/or uninstall FIKA files depending on `--launchmode`. True if provided, false if not. |
| `--quick` | `/quick` | Will skip every setup and will not change IPs. Has priority over `--setup` if both are provided. |
| `--ip-index` | just a lone number, like `2` | Will use the IP in the specified index for mp. Is 1-indexed. Will request user input if `--launchmode mp` was provided and `--ip-index` wasn't |
| `--dry-run` | Not available | Will run the code without actually executing anything. Included for debug purposes. |
| `--log` | Not available | Allows the `log()` function to work if you set any of them along the code.Both of the args above do not exist in the **.bat** edition because I prefer to keep my sanity. |
* ``--launchmode`` **values**:
  * `sp` / Singleplayer
  * `mpc` / Multiplayer Client
  * `mph` / Multiplayer Host

### Examples

```
FikOffCS.exe --launchmode mpc --ip-index 3
FikOff.py --launchmode sp --setup --ip-index 1
FikOff.bat /mph 1 /setup      - note: this must be in this exact order. ip-index can be omitted (defaults to line 2). 
                              - /setup can be exchanged with /quick
```

# Troubleshooting

❓ **FikOff doesn't work**  
- Check if your paths are correct  
- Make sure your antivirus didn’t block it  

❓ **IP doesn't update in the launcher**  
- Enable **developer mode** in the SPT Launcher  

❓ **FikOff is telling me to, well, "FikOff™":**
- Congratulations. You probably mistyped a basic CLI option.

# Disclaimers

FikOff aims to be a straightforward tool that works reliably. While it may not win any coding awards, it has been refactored since its conception to be less of a *"steaming hot pile of shit"* and is now much easier to understand and maintain.


I aim to keep most functionality I can when porting FikOff to other programming languages, but due to different *capabilities* and *constraints*, I unfortunately cannot port *everything*. Below is a list mentioning what each version of FikOff **has and doesn't have**.

* Python
  * OG Codebase. Has everything.
* C#
  * Apart from a few sleep() calls in the Interactive Mode that were added to try and keep the output readable, the C# edition of FikOff also has everything.
* Batch. Here is where things get messy.
  * Has: CLAP args, auto server and launcher start, auto setup (IP setup included), the ability to run on a fresh Windows install.
  * Hasn't: Checks to see if the server is actually online before starting the launcher, **automatically turning** developer mode in the launcher to on, support for multiple languages, funny flavor texts (this one's the saddest out of these tbh).

### How to contribute

* Fork this repo
* Do your thing
* Open a pull request with a short description

When adding new localizations, follow JSON formatting rules and **don’t remove `\n` line breaks** unless you want your terminal to look like your PMC after being hit by Tagilla's sledgehammer.

### About AI usage

The only times were AI was used in this project were to:

  * Refactor hardcoded strings into language-based lookups (because I am a moron and forgot to do that);
  * Give me a draft markdown file that eventually turned into this mess of a 'readme';
  * Translate all of this shit from Python to a language that can be compiled into something that *should* take no more than a double click to start;
  * Translate strings to languages other than English and *True Portuguese™.*
    * PS: You're still welcome to submit translation PRs — just note what’s broken or missing.

### To-do

  * ~~Add command-line arguments so playnite users such as myself can just start their game without having to see an ugly terminal~~ Done.
  * Port this to other languages so y'all can pick your poison
    * Currently ported to: C#, Batch, and Python (OG code was made in Python). 
    * Of these, AI was only used to port the code from Python to: C#

### Fun fact

The script may make fun of you or curse at you. Especially if you mess up your input. That's just tough love. Enjoy.

## License
This project is licensed under the [MIT License](LICENSE).
