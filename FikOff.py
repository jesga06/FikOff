import shutil as sh
import os
import subprocess
import time
import random
import json
import socket
import argparse
import sys
from urllib.parse import urlparse
class Main:
    def __init__(self):
        self.launchmodes = {
               'sp': 'sp',
               'mpc': 'mpc',
               'mph': 'mph'
               }
        parser = argparse.ArgumentParser()
        parser.add_argument('--launchmode', choices=[mode for mode in self.launchmodes.keys()], help="Determines the launch sequence that the script will play out.")
        parser.add_argument('--quick', action='store_true', help="Quick launch mode. Will not set up anything nor change IP addresses.")
        parser.add_argument('--setup', action='store_true', help="Executes the setup routine (Installs/Uninstalls FIKA and changes IP). Will only change IP if not present.")
        parser.add_argument('--ip-index', type=int, help="Sets the index of the MP IP (from the ip.txt file, 1-indexed), bypassing user input.")
        parser.add_argument('--dry-run', action='store_true', help="Doesn't open nor setups anything if true.")
        parser.add_argument('--log', action='store_true', help="Activates logging.") # useful for contributors
        self.args = parser.parse_args()
        self.dry_run = self.args.dry_run
        self.log_enabled = self.args.log # set to True to enable logging without --log
        self.logs = []
        # codeblock that tries to open the lang file
        lang_path = os.path.join(os.path.dirname(__file__), "lang.json")
        try:
            self.log("trying to open lang.json")
            with open(lang_path, "r", encoding="utf-8") as f:
                langdata = json.load(f)
        except UnicodeDecodeError:
            self.log("UnicodeDecodeError")
            with open(lang_path, "r", encoding="utf-8-sig") as f:
                langdata = json.load(f)

        selected = langdata.get("selected")
        available_langs = [k for k in langdata if isinstance(langdata[k], dict)]
        if selected in available_langs:
            self.log("selected language found in lang.json")
            self.strings = langdata[selected]
        elif available_langs:
            self.log("selected language not found in lang.json. using first available language.")
            self.strings = langdata[available_langs[0]]
        else:
            raise ValueError

        self.rng = [random.randint(0, len(self.strings["svmsg"]) - 1) for _ in range(3)]
        self.log(f"random numbers generated: {self.rng}")
        self.svmsg = self.strings["svmsg"]
        self.lmsg = self.strings["lmsg"]
        self.endmsg = self.strings["endmsg"]

        self.currentDir = os.path.dirname(__file__)
        self.paths = {
            "pluginFikaUnins": os.path.join(self.currentDir, "_fika", "BepInEx", "plugins", "Fika.Core.dll"),
            "pluginFikaIns": os.path.join(self.currentDir, "BepInEx", "plugins", "Fika.Core.dll"),
            "modFikaUnins": os.path.join(self.currentDir, "_fika", "user", "mods", "fika-server"),
            "modFikaIns": os.path.join(self.currentDir, "user", "mods", "fika-server"),
            "config": os.path.join(self.currentDir, "user", "launcher", "config.json"),
            "launcher": os.path.join(self.currentDir, "SPT.Launcher.exe"),
            "server": os.path.join(self.currentDir, "SPT.Server.exe"),
            "ip_file": os.path.join(self.currentDir, "ip.txt")
            }

        self.svip = None # init as None
        if os.path.exists(self.paths["ip_file"]):
            self.log("ip.txt found")
            self.ipFileExists = True
            self.singleplayer_ip = ""
            self.multiplayer_ips = []
            try:
                self.log("calling get_ips()")
                self.get_ips()
            except Exception as error:
                self.log(f"ip.txt error: {error}")
                print(self.strings["file_error"].format(error=error))
                self.singleplayer_ip = "https://127.0.0.1:6969"
                self.multiplayer_ips = []
        else:
            self.log("ip.txt not found")
            print(self.strings["ip_not_found"])
            self.ipFileExists = False
            self.singleplayer_ip = "https://127.0.0.1:6969"
            self.multiplayer_ips = []

    def get_ips(self):
        self.log("get_ips() - reading ip.txt")
        with open(self.paths["ip_file"], 'r', encoding='utf-8') as file:
            iplist = [ip.strip() for ip in file.readlines() if ip.strip()]
            if iplist:
                self.log(f"get_ips() - found {len(iplist)} IPs")
                self.singleplayer_ip = iplist[0]
                self.multiplayer_ips = iplist[1:]
            else:
                self.log("get_ips() - ip.txt is empty")
                self.singleplayer_ip = "https://127.0.0.1:6969"
                self.multiplayer_ips = []

    def log(self, msg):
        if self.log_enabled:
            log_msg = f"[LOG] {msg}"
            self.logs.append(log_msg)
            print(log_msg)

    def cSA(self, ip, port, timeout_attempt=2):
        """
        'checkSingleAttempt'
        Checks if the server is alive and running. Returns True if alive, False if not.\n
        I haven't had networking classes yet, so I'm terribly sorry if this code is shit
        """
        self.log(f"cSA() - attempting to connect to {ip}:{port} with timeout {timeout_attempt}s")
        try:
            with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as sock:
                sock.settimeout(timeout_attempt)
                result = sock.connect_ex((ip, port))
                if result == 0:
                    self.log(f"cSA() - successfully connected to {ip}:{port}")
                    return True
                else:
                    self.log(f"cSA() - failed to connect to {ip}:{port} - error code: {result}")
                    return False
        except socket.timeout:
            self.log(f"cSA() - connection to {ip}:{port} timed out after {timeout_attempt}s.")
            return False
        except Exception as error:
            self.log(f"cSA() - an error occurred while checking {ip}:{port} - {error}")
            return False

    def try_connection(self, parsed_url):
        """
        Checks for server status every 5 seconds for 420 seconds.\n
        Uses cSA() for each try
        """
        interval = 5.0
        total_timeout = 420.0
        if not parsed_url or not parsed_url.hostname or not parsed_url.port:
            self.log(f"try_connection() - error: parsed_url is invalid: {parsed_url}")
            return 'ParsingError'
        ip = parsed_url.hostname
        port = parsed_url.port
        full_address = f"{ip}:{port}"
        self.log(f"try_connection() - starting check for {full_address}")
        start_time = time.time()
        print(self.strings["server_timeout_begin"].format(ip=full_address))
        while time.time() - start_time < total_timeout:
            elapsed_time = time.time() - start_time
            self.log(f"try_connection() - elapsed: {elapsed_time:.2f}s. trying to connect to {full_address}")
            print(self.strings["server_timeout_timer"].format(ip=full_address, interval=interval+2, secs=int(elapsed_time)))
            if self.cSA(ip, port):
                self.log(f"try_connection() - server {full_address} responded")
                print(self.strings["server_started"].format(ip=full_address))
                return True
            time.sleep(interval)
        self.log(f"try_connection() - total timeout reached ({total_timeout}s). Server {full_address} did not come online.")
        return 'TimeoutHit'

    def input_prompt(self, prompt=str, valid=list, retryprompt=None):
        """
        this was created because i couldnt stand having to write ``input()`` and input validations and loops over and over again on ``start()``\n
        `prompt` and `retryprompt` must be in `"string_name"` format, not `self.strings["string_name"]`
        """
        self.log(f"input_prompt() - called with prompt '{prompt}'")
        value = input(self.strings[f"{prompt}"]).upper()
        while value not in valid:
            if value in ['Q', 'QUIT', 'EXIT', 'KILL']:
                sys.exit()
            self.log(f"input_prompt() - invalid input '{value}'")
            print(self.strings[f"error_{prompt}"], "\n")
            time.sleep(1)
            if retryprompt:
                self.log("input_prompt() - retry prompt called")
                value = input(self.strings[f"{retryprompt}"]).upper()
            else:
                self.log("input_prompt() - no retry prompt, recalling prompt")
                value = input(self.strings[f"{prompt}"]).upper()
        self.log(f"input_prompt() - valid input '{value}'")
        return value

    def show_end_message(self):
        print() # newline
        self.log("show_end_message() - called")
        print(self.endmsg[self.rng[2]], "\n" + self.strings["thanks"])
        input(self.strings["press_enter"])
        sys.exit()

    def write_config_ip(self, ip=str): # reason of creation is analogue to input_prompt()
        self.log(f"write_config_ip() - called with ip='{ip}'")
        config_path = self.paths["config"]
        try:
            with open(config_path, "r", encoding="utf-8") as file:
                self.log("write_config_ip() - reading and parsing config.json")
                data = json.load(file)

            self.log("write_config_ip() - modifying data in memory")
            data["Url"] = ip
            data["IsDevMode"] = True

            with open(config_path, "w", encoding="utf-8") as file:
                self.log("write_config_ip() - writing modified data back to config.json")
                json.dump(data, file, indent=4) # keeping indent

        except FileNotFoundError:
            self.log(f"write_config_ip() - config file not found at {config_path}")
            print(self.strings["config_not_found"].format(path=config_path))
        except (KeyError, json.JSONDecodeError) as e:
            self.log(f"write_config_ip() - error processing JSON file: {e}")
            print(self.strings["config_key_error"].format(error=e))

    def perform_setup(self, type):
        # this function now ONLY handles the setup routine (copying/removing mods).
        self.log(f"perform_setup() - called for type '{type}'")
        if type in ['mph', 'mpc']:
            self.log("perform_setup() - calling copy() for MP.")
            if not self.dry_run:
                self.copy()
        elif type == 'sp':
            self.log("perform_setup() - calling remove_fika() for SP.")
            if not self.dry_run:
                self.remove_fika()

    def start_processes(self, type):
        # this function now ONLY handles starting the processes and waiting for the server.
        self.log(f"start_processes() - called for type '{type}'")
        if type in ['sp', 'mph']:
            self.log(f"start_processes() - mode is '{type}', starting server.")
            print(self.strings["starting_server"])
            print(self.svmsg[self.rng[0]])
            if not self.dry_run:
                self.log("start_processes() - dry_run is false, starting server process.")
                os.startfile(self.paths["server"])
                self.log("start_processes() - calling try_connection()")
                result = self.try_connection(self.svip)
                if result is not True:
                    self.log(f"start_processes() - try_connection failed with result: {result}")
                    error_map = {
                        'ParsingError': self.strings["parsing_error"],
                        'TimeoutHit': self.strings["server_timeout_end"]
                    }
                    print(error_map.get(result, self.strings["server_error_mystical"]))

        self.log("start_processes() - starting launcher.")
        print(self.strings["starting_launcher"])
        print(self.lmsg[self.rng[1]])
        if not self.dry_run:
            self.log("start_processes() - dry_run is false, starting launcher process.")
            subprocess.Popen(["cmd", "/c", "start", "", self.paths["launcher"]], shell=True, cwd=self.currentDir)

    def launcher_ip(self, type=str):
        self.log(f"launcher_ip() - called for type '{type}'")
        time.sleep(1)
        print(self.strings["changing_ip"])
        time.sleep(0.5)
        if not os.path.exists(self.paths["config"]):
            self.log("launcher_ip() - config not found")
            print(self.strings["config_not_found"].format(path=self.paths["config"]))
            return

        target_ip = None
        # the curses that past me have bestowed upon myself have been lifted
        # i now have been enlightened by my own mistakes and have understood once again what dark algorithms the below codeblock executes.
        if type == "sp":
            self.log("launcher_ip() - type is SP, setting target_ip to singleplayer_ip")
            target_ip = self.singleplayer_ip
        elif type in ['mph', 'mpc']:
            self.log("launcher_ip() - type is MP, determining target_ip")
            if self.args.ip_index is not None:
                self.log(f"launcher_ip() - --ip-index provided with value {self.args.ip_index}")
                try:
                    selected_index = self.args.ip_index - 1
                    if 0 <= selected_index < len(self.multiplayer_ips):
                        target_ip = self.multiplayer_ips[selected_index]
                        self.log(f"launcher_ip() - index is valid, IP selected: {target_ip}")
                        print(self.strings["ip_selected"].format(num=self.args.ip_index, ip=target_ip))
                    else:
                        self.log("launcher_ip() - index is out of range")
                        print(self.strings["invalid_ip_index"] + "\n" + self.strings["index_range"].format(range=len(self.multiplayer_ips)))
                except IndexError:
                    self.log("launcher_ip() - caught IndexError, likely ip_index issue")
                    print(self.strings["index_missing"])
            elif len(self.multiplayer_ips) == 0:
                self.log("launcher_ip() - no MP IPs found, defaulting to SP IP")
                target_ip = self.singleplayer_ip
                print(self.strings["ip_missing_ext"].format(ip=target_ip))
            elif len(self.multiplayer_ips) == 1:
                self.log("launcher_ip() - only one MP IP found, selecting it automatically")
                target_ip = self.multiplayer_ips[0]
            else:
                self.log(f"launcher_ip() - {len(self.multiplayer_ips)} MP IPs found, prompting user")
                print(self.strings["multiple_ips"])
                time.sleep(1)
                for index, ip in enumerate(self.multiplayer_ips, 1):
                    print(f"{index} - {ip}")
                    time.sleep(0.5)
                while True: # vai se foder andré renato
                    try:
                        num = int(input(self.strings["select_ip"]))
                        if 1 <= num <= len(self.multiplayer_ips):
                            target_ip = self.multiplayer_ips[num - 1]
                            print(self.strings["ip_selected"].format(num=num, ip=target_ip))
                            print() # newline
                            time.sleep(0.5)
                            break
                        else:
                            print(self.strings["error_select_ip_index"].format(range=len(self.multiplayer_ips)))
                            time.sleep(2)
                            print() # newline
                    except ValueError:
                        print(self.strings["error_select_ip_input"])
                        time.sleep(2)
                        print() # newline

        if target_ip:
            self.log(f"launcher_ip() - final target IP set to {target_ip}")
            self.svip = urlparse(target_ip)
            self.log(f"launcher_ip() - self.svip parsed as: {self.svip}")
            if not self.dry_run:
                self.write_config_ip(target_ip)
        else:
            self.log("launcher_ip() - no target_ip could be determined. svip not set.")

    def copy(self): # kinda self explanatory
        self.log("copy() - called")
        print(self.strings["deleting_files"])
        try:
            self.log("copy() - calling remove_fika() silently")
            self.remove_fika(silent=True)
        except (IOError, OSError, sh.Error) as error:
            self.log(f"copy() - error during silent remove_fika: {error}")
            pass
        print(self.strings["residues_removed"])
        time.sleep(.5)
        print(self.strings["installing_fika"])
        try:
            if not self.dry_run:
                self.log("copy() - dry-run is false, copying files")
                sh.copytree(self.paths["modFikaUnins"], self.paths["modFikaIns"])
                sh.copy(self.paths["pluginFikaUnins"], self.paths["pluginFikaIns"])
            print(self.strings["install_success"], "\n")
        except (IOError, OSError, sh.Error) as error:
            self.log(f"copy() - error during copytree/copy: {error}")
            print(self.strings["install_fail"].format(error=error))
        time.sleep(1)

    def remove_fika(self, silent=False): # same as above
        self.log(f"remove_fika() - called (silent={silent})")
        try:
            if not silent:
                print(self.strings["uninstalling_fika"])
            if not self.dry_run:
                self.log("remove_fika() - dry-run is false, removing files")
                if os.path.exists(self.paths["pluginFikaIns"]):
                    os.remove(self.paths["pluginFikaIns"])
                    self.log("remove_fika() - removed plugin file.")
                if os.path.exists(self.paths["modFikaIns"]):
                    sh.rmtree(self.paths["modFikaIns"])
                    self.log("remove_fika() - removed mod directory.")
            time.sleep(1)
            if not silent:
                print(self.strings["uninstall_success"], "\n")
        except (IOError, OSError, sh.Error) as error:
            self.log(f"remove_fika() - error: {error}")
            if not silent:
                print(self.strings["uninstall_fail"].format(error=error))
            return error
        time.sleep(1)

    def start(self):
        # this function handles the interactive mode.
        self.log("start() - interactive mode started")
        print(self.strings["help_start_prompt"])
        time.sleep(1.5)
        mode_input = self.input_prompt("start_prompt", ['SP', 'MPH', 'MPC', 'QSP', 'QMPH', 'QMPC'], "retry_start_prompt")
        print() # newline
        is_quick = mode_input.startswith('Q')
        game_mode = mode_input[1:].lower() if is_quick else mode_input.lower()
        self.log(f"start() - mode selected: {game_mode}, quick: {is_quick}")

        self.launcher_ip(game_mode)

        if not is_quick:
            self.log("start() - normal mode detected, performing setup.")
            self.perform_setup(game_mode)
            autostart_input = self.input_prompt("auto_start_prompt", ['Y', 'N', 'FIKOFF'])
            if autostart_input == 'Y':
                self.log("start() - user chose to start processes.")
                self.start_processes(game_mode)
        else:
            self.log("start() - quick mode detected, skipping setup.")
            print(self.strings["start_quick"])
            self.start_processes(game_mode)
        
        self.show_end_message()

main = Main()
main.log("main() initialized")

if main.args.launchmode is not None:
    # routine if CL-args were provided
    mode = main.args.launchmode
    main.log(f"--launchmode provided. mode: {mode}, quick: {main.args.quick}, setup: {main.args.setup}")

    # setting the IP address. in quick mode, this only sets self.svip without writing to config.
    if not main.args.quick:
        main.launcher_ip(mode)
    else:
        # determine svip without changing the config file.
        # this is so try_connection() doesn't hang trying to connect to the wrong address
        ip_to_parse = main.singleplayer_ip
        if mode in ['mph', 'mpc'] and main.multiplayer_ips:
            ip_to_parse = main.multiplayer_ips[0] # defaults to the first one
            if main.args.ip_index is not None:
                if 0 <= (main.args.ip_index - 1) < len(main.multiplayer_ips): # checks if index is valid
                    ip_to_parse = main.multiplayer_ips[main.args.ip_index - 1]
                else:
                    main.log(f"quick mode: --ip-index {main.args.ip_index} is out of range. falling back to first IP.")
                    print(main.strings["invalid_ip_index"])
                    print(main.strings["invalid_ip_index_fallback"].format(ip=ip_to_parse))
        main.svip = urlparse(ip_to_parse)
        main.log(f"quick mode: svip set to {main.svip}")

    # calls perform_setup() if --setup is present and --quick isnt
    if main.args.setup and not main.args.quick:
        main.log("--setup flag detected.")
        main.perform_setup(mode)
    
    main.log("calling start_processes()")
    main.start_processes(mode)

else:
    # interactive mode (no --launchmode argument was provided).
    main.log("no --launchmode provided. Defaulting to interactive mode.")
    try:
        main.start()
    except ValueError as error:
        main.log(f"configuration error in interactive mode: {error}")
        print("No valid languages found in lang.json.")
    except KeyError as error:
        main.log(f"configuration error in interactive mode: {error}")
        print(f"Error loading configuration: {error}")
    except Exception as error:
        main.log(f"something went to shit in interactive mode: {error}")
        print(main.strings["mystical_error"].format(error=error))
        input(main.strings["press_enter"]) 
