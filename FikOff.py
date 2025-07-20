import shutil as sh
import os
import subprocess
import time
import random
import json
import argparse
import sys
class Main:
    def __init__(self):
        self.launchmodes = {
               'sp': 'sp',
               'mpc': 'mpc',
               'mph': 'mph'
               }
        parser = argparse.ArgumentParser()
        parser.add_argument('--launchmode', choices=[mode for mode in self.launchmodes.keys()], help="Launch mode: sp, mph, mpc.")
        parser.add_argument('--quick', action='store_true', help="Quick launch mode. Will not set up anything.")
        parser.add_argument('--setup', action='store_true', help="Executes the setup routine (Installs/Uninstalls FIKA).")
        parser.add_argument('--ip-index', type=int, help="Sets the index of the MP IP (from the ip.txt file, 1-indexed), bypassing user input.")
        parser.add_argument('--dry-run', action='store_true', help="Doesn't open anything if true.")
        parser.add_argument('--log', action='store_true', help="Activates logging.") # useful for contributors
        self.args = parser.parse_args()
        self.dry_run = self.args.dry_run
        self.log_enabled = self.args.log
        self.logs = []
        # codeblock that tries to open the lang file
        lang_path = os.path.join(os.path.dirname(__file__), "lang.json")
        try:
            with open(lang_path, "r", encoding="utf-8") as f:
                langdata = json.load(f)
        except UnicodeDecodeError:
            with open(lang_path, "r", encoding="utf-8-sig") as f:
                langdata = json.load(f)

        selected = langdata.get("selected")
        available_langs = [k for k in langdata if isinstance(langdata[k], dict)]
        if selected in available_langs:
            self.strings = langdata[selected]
        elif available_langs:
            self.strings = langdata[available_langs[0]]
        else:
            raise ValueError("No valid languages found in lang.json.")

        self.rng = [random.randint(0, len(self.strings["svmsg"]) - 1) for _ in range(3)]
        self.svmsg = self.strings["svmsg"]
        self.lmsg = self.strings["lmsg"]
        self.endmsg = self.strings["endmsg"]

        self.currentDir = os.path.dirname(__file__)
        self.paths = {
            "pluginFikaUnins": self.currentDir + "\\_fika\\BepInEx\\plugins\\Fika.Core.dll",
            "pluginFikaIns": self.currentDir + "\\BepInEx\\plugins\\Fika.Core.dll",
            "modFikaUnins": self.currentDir + "\\_fika\\user\\mods\\fika-server",
            "modFikaIns": self.currentDir + "\\user\\mods\\fika-server",
            "config": self.currentDir + "\\user\\launcher\\config.json",
            "launcher": self.currentDir + "\\SPT.Launcher.exe",
            "server": self.currentDir + "\\SPT.Server.exe",
            "ip_file": self.currentDir + "\\ip.txt" # double backslashes were used because tarkov doesn't run on linux AFAIK
        }
        if os.path.exists(self.paths["ip_file"]):
            self.ipFileExists = True
            self.singleplayer_ip = ""
            self.multiplayer_ips = []
            try:
                self.get_ips()
            except Exception:
                print(self.strings["file_error"])
                self.singleplayer_ip = "https://127.0.0.1:6969"
                self.multiplayer_ips = []
        else:
            print(self.strings["ip_not_found"])
            self.ipFileExists = False
            self.singleplayer_ip = "https://127.0.0.1:6969"
            self.multiplayer_ips = []

    def get_ips(self):
        with open(self.paths["ip_file"], 'r', encoding='utf-8') as file:
            iplist = [ip.strip() for ip in file.readlines() if ip.strip()]
            if iplist:
                print(self.strings["ip_found"])
                self.singleplayer_ip = iplist[0]
                self.multiplayer_ips = iplist[1:]
            else:
                print(self.strings["ip_not_found"])
                self.singleplayer_ip = "https://127.0.0.1:6969"
                self.multiplayer_ips = []

    def log(self, msg):
        if self.log_enabled:
            self.logs.append(msg)
            print("[LOG]", msg)

    def input_prompt(self, prompt=str, valid=list, retryprompt=None):
        """this was created because i couldnt stand having to write ``input()`` and input validations and loops over and over again on ``start()``\n
        `prompt` and `retryprompt` must be in `"string_name"` format, not `self.strings["string_name"]`"""
        value = input(self.strings[f"{prompt}"]).upper()
        while value not in valid:
            print(self.strings[f"error_{prompt}"])
            time.sleep(1)
            if retryprompt:
                value = input(self.strings[f"{retryprompt}"]).upper()
            else:
                value = input(self.strings[f"{prompt}"]).upper()
        return value
                    

    def show_end_message(self):
        print(self.endmsg[self.rng[2]], "\n" + self.strings["thanks"])
        input(self.strings["press_enter"])
        sys.exit()

    def write_config_ip(self, ip=str):
        """reason of creation is analogue to `input_prompt()`"""
        with open(self.paths["config"], "r", encoding="utf-8") as file:
            lines = file.readlines()
        with open(self.paths["config"], "w", encoding="utf-8") as file:
            for line in lines:
                if '"Url":' in line:
                    pieces = line.split(":", 1)
                    key = pieces[0] # keeps indentation
                    new_line = f'{key}: "{ip}"\n'
                    file.write(new_line)
                else:
                    file.write(line)

    def launchSequence(self, type, quick=None):
        """Calls launcher_ip() with the same type argument and starts only the launcher or the server and launcher depending on the type passed.\n
        Defaults to 'sp'."""
        type = type.lower() # can never be too paranoid
        if quick:
            if not self.dry_run:
                if type in ['sp', 'mph']:
                    os.startfile(self.paths["server"])
                    subprocess.Popen(["cmd", "/c", "start", "", self.paths["launcher"]], shell=True, cwd=self.currentDir)
                else: #if type == mpc aka "i will not need the server please just start the game already i wanna die to Tagilla"
                    subprocess.Popen(["cmd", "/c", "start", "", self.paths["launcher"]], shell=True, cwd=self.currentDir)
            return # ends early in case of --launchmode quick
        print() # newline to make the code more bearable to read
        self.launcher_ip(type)
        if type == 'mph':
            if not self.dry_run:
                self.copy()
            print(self.strings["starting_server"])
            print(self.svmsg[self.rng[0]])
            if not self.dry_run:
                os.startfile(self.paths["server"])
                time.sleep(10)
        elif type == 'mpc':
            if not self.dry_run:
                self.copy()
        else: # is sp
            if not self.dry_run:
                self.removeFika()
                os.startfile(self.paths["server"])
        print(self.strings["starting_launcher"])
        print(self.lmsg[self.rng[1]])
        if not self.dry_run:
            subprocess.Popen(["cmd", "/c", "start", "", self.paths["launcher"]], shell=True, cwd=self.currentDir)

    def launcher_ip(self, type):
        time.sleep(1)
        print(self.strings["changing_ip"])
        time.sleep(0.5)
        if not os.path.exists(self.paths["config"]):
            print(self.strings["config_not_found"].format(path={self.paths["config"]}))
            return

        target_ip = self.singleplayer_ip if type == "sp" else None
        # the curses that past me have bestowed upon myself have been lifted
        # i now have been enlightened by my own mistakes and have understood once again what dark algorithms the below codeblock executes.
        if type in ['mph', 'mpc']:
            if self.args.ip_index is not None: # checks for the --ip-index arg
                try:
                    selected_index = self.args.ip_index - 1 # --ip-index is 1-indexed, python lists are 0-indexed
                    if 0 <= selected_index < len(self.multiplayer_ips): # checks if the index is within range
                        target_ip = self.multiplayer_ips[selected_index]
                        print(self.strings["ip_selected"].format(num=self.args.ip_index, ip=target_ip), "\n") # i should've programmed this to just tell the user to FikOff™
                        time.sleep(1)
                    else:
                        print(self.strings["invalid_ip_index"], "\n" + self.strings["index_range"].format(range=len(self.multiplayer_ips)), "\n" + self.strings["ip_missing_ext"])
                        time.sleep(1)
                except: # i dont think this ever gets executed tbh
                    print(self.strings["index_missing"], "\n")
                    time.sleep(1)
            elif len(self.multiplayer_ips) == 1:
                target_ip = self.multiplayer_ips[0]
            elif len(self.multiplayer_ips) == 0:
                target_ip = self.singleplayer_ip
                print(self.strings["ip_missing_ext"])
            else:
                print(self.strings["multiple_ips"])
                time.sleep(1)
                for index, ip in enumerate(self.multiplayer_ips, 1):
                    print(f"{index} - {ip}")
                    time.sleep(0.2)
                time.sleep(1)
                while True: # vai se foder andré renato
                    try:
                        num = int(input(self.strings["select_ip"]))
                        if 1 <= num <= len(self.multiplayer_ips):
                            target_ip = self.multiplayer_ips[num - 1]
                            print(self.strings["ip_selected"].format(num=num, ip=target_ip), "\n")
                            time.sleep(1)
                            break
                        else:
                            print(self.strings[f"error_select_ip_index"], "\n")
                    except:
                        print(self.strings["error_select_ip_input"], "\n")
                        time.sleep(1)
        self.write_config_ip(target_ip)

    def copy(self): # kinda self explanatory
        print("\n", self.strings["deleting_files"])
        try:
            self.removeFika(silent=True)
        except:
            pass
        print(self.strings["residues_removed"])
        time.sleep(.5)
        print("\n", self.strings["installing_fika"])
        try:
            if not self.dry_run:
                sh.copytree(self.paths["modFikaUnins"], self.paths["modFikaIns"])
                sh.copy(self.paths["pluginFikaUnins"], self.paths["pluginFikaIns"])
            print(self.strings["install_success"], "\n")
        except Exception as erro:
            print(self.strings["install_fail"] + f"\nError: {erro}")
        time.sleep(1)

    def removeFika(self, silent=False): # same as above
        try:
            if not silent:
                print(self.strings["uninstalling_fika"])
            if not self.dry_run:
                os.remove(self.paths["pluginFikaIns"])
                sh.rmtree(self.paths["modFikaIns"])
            time.sleep(1)
            if not silent:
                print(self.strings["uninstall_success"], "\n")
        except:
            pass
        time.sleep(1)

    def start(self):
        time.sleep(1)
        print() # newline
        print(self.strings["help_start_prompt"])
        time.sleep(1.5)
        mode = self.input_prompt("start_prompt", ['SP', 'MPH', 'MPC', 'QSP', 'QMPH', 'QMPC'], "retry_start_prompt")
        if mode.startswith('Q'):
            print(self.strings["start_quick"])
            self.launchSequence(mode[1:].lower(), quick=True)
        else:
            self.launchSequence(mode.lower())
            self.show_end_message()

main = Main()

# argument parsing
parser = argparse.ArgumentParser()
parser.add_argument('--launchmode', choices=[mode for mode in main.launchmodes.keys()], help="Launch mode: sp, mph, mpc.")
parser.add_argument('--quick', action='store_true', help="Quick launch mode. Will not set up anything.")
parser.add_argument('--setup', action='store_true', help="Executes the setup routine (Installs/Uninstalls FIKA).")
parser.add_argument('--ip-index', type=int, help="Sets the index of the MP IP (from the ip.txt file, 1-indexed), bypassing user input.")
parser.add_argument('--dry-run', action='store_true', help="Doesn't open anything if true.")
parser.add_argument('--log', action='store_true', help="Activates logging.") # useful for contributors
args = parser.parse_args()

if args.launchmode in main.launchmodes:
    if args.launchmode == 'sp':
        if args.setup:
            try:
                main.removeFika(silent=True)
            except Exception as error:
                if not args.quick:
                    print(main.strings["uninstall_fail"] + f"\nError: {error}")
    elif args.launchmode in ['mpc', 'mph']:
        if args.setup:
            try:
                main.copy()
            except Exception as error:
                if not args.quick:
                    print(main.strings["install_fail"] + f"\nError: {error}")
    main.launchSequence(main.launchmodes[args.launchmode], quick=args.quick)
else:
    try:
        main.start()
    except KeyError as e:
        # defaults to english because im pretty sure most of the people using this also speak english
        print("Caught a KeyError. Maybe you forgot to extract the lang.json file or deleted something by accident?\n" \
        f"Missing key: {e}")