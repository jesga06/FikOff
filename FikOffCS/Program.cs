#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

public class Lang
{
    public List<string> EndMsg { get; set; } = new List<string>();
    public List<string> LMsg { get; set; } = new List<string>();
    public List<string> SvMsg { get; set; } = new List<string>();
    public string Help_Start_Prompt { get; set; } = "";
    public string Start_Prompt { get; set; } = "";
    public string Error_Start_Prompt { get; set; } = "";
    public string Retry_Start_Prompt { get; set; } = "";
    public string Auto_Start_Prompt { get; set; } = "";
    public string Error_Auto_Start_Prompt { get; set; } = "";
    public string Select_Ip { get; set; } = "";
    public string Error_Select_Ip_Input { get; set; } = "";
    public string Error_Select_Ip_Index { get; set; } = "";
    public string Ip_Found { get; set; } = "";
    public string Ip_Not_Found { get; set; } = "";
    public string Ip_Missing { get; set; } = "";
    public string File_Error { get; set; } = "";
    public string Config_Not_Found { get; set; } = "";
    public string Ip_Missing_Ext { get; set; } = "";
    public string Ip_Selected { get; set; } = "";
    public string Multiple_Ips { get; set; } = "";
    public string Parsing_Error { get; set; } = "";
    public string Index_Missing { get; set; } = "";
    public string Index_Range { get; set; } = "";
    public string Invalid_Ip_Index { get; set; } = "";
    public string Starting_Server { get; set; } = "";
    public string Server_Timeout_Begin { get; set; } = "";
    public string Server_Timeout_Timer { get; set; } = "";
    public string Server_Started { get; set; } = "";
    public string Server_Timeout_End { get; set; } = "";
    public string Server_Error_Mystical { get; set; } = "";
    public string Starting_Launcher { get; set; } = "";
    public string Changing_Ip { get; set; } = "";
    public string Deleting_Files { get; set; } = "";
    public string Residues_Removed { get; set; } = "";
    public string Installing_Fika { get; set; } = "";
    public string Install_Success { get; set; } = "";
    public string Install_Fail { get; set; } = "";
    public string Uninstalling_Fika { get; set; } = "";
    public string Uninstall_Fail { get; set; } = "";
    public string Uninstall_Success { get; set; } = "";
    public string Invalid_Launchmode { get; set; } = "";
    public string Thanks { get; set; } = "";
    public string Press_Enter { get; set; } = "";
    public string Start_Quick { get; set; } = "";
}

public class Options
{
    public string LaunchMode { get; set; }
    public bool Quick { get; set; }
    public bool Setup { get; set; }
    public int? IpIndex { get; set; }
    public bool DryRun { get; set; }
    public bool Log { get; set; }
}

public class FikOff
{
    private readonly Options _args;
    private readonly bool _dryRun;
    private readonly bool _logEnabled;
    private readonly List<string> _logs = new List<string>();
    private readonly Lang _strings;
    private readonly Random _random = new Random();
    private readonly int[] _rng;
    private readonly string _currentDir;
    private readonly Dictionary<string, string> _paths;
    private Uri _svip;

    private bool _ipFileExists;
    private string _singlePlayerIp = "https://127.0.0.1:6969";
    private List<string> _multiplayerIps = new List<string>();

    public FikOff(Options args)
    {
        _args = args;
        _dryRun = args.DryRun;
        _logEnabled = args.Log;

        _currentDir = AppContext.BaseDirectory;
        var langPath = Path.Combine(_currentDir, "lang.json");

        try
        {
            Log("trying to open lang.json");
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var langDataRaw = File.ReadAllText(langPath, Encoding.UTF8);
            var langDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(langDataRaw, jsonOptions);

            string selectedLangKey = "en";
            if (langDict.TryGetValue("selected", out var selectedLangElement))
            {
                selectedLangKey = selectedLangElement.GetString() ?? "en";
                Log("selected language found in lang.json");
            }
            else
            {
                Log("selected language not found in lang.json. using first available language.");
            }

            if (langDict.TryGetValue(selectedLangKey, out var langElement))
            {
                _strings = JsonSerializer.Deserialize<Lang>(langElement.GetRawText(), jsonOptions);
            }

            if (_strings == null)
            {
                throw new InvalidOperationException($"Language '{selectedLangKey}' not found or is invalid in lang.json.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading lang.json from '{langPath}': {ex.Message}");
            throw;
        }

        _rng = new[] {
            _strings.SvMsg.Any() ? _random.Next(0, _strings.SvMsg.Count) : 0,
            _strings.LMsg.Any() ? _random.Next(0, _strings.LMsg.Count) : 0,
            _strings.EndMsg.Any() ? _random.Next(0, _strings.EndMsg.Count) : 0
        };
        Log($"random numbers generated: [{string.Join(", ", _rng)}]");

        _paths = new Dictionary<string, string>
        {
            { "pluginFikaUnins", Path.Combine(_currentDir, "_fika", "BepInEx", "plugins", "Fika.Core.dll") },
            { "pluginFikaIns", Path.Combine(_currentDir, "BepInEx", "plugins", "Fika.Core.dll") },
            { "modFikaUnins", Path.Combine(_currentDir, "_fika", "user", "mods", "fika-server") },
            { "modFikaIns", Path.Combine(_currentDir, "user", "mods", "fika-server") },
            { "config", Path.Combine(_currentDir, "user", "launcher", "config.json") },
            { "launcher", Path.Combine(_currentDir, "SPT.Launcher.exe") },
            { "server", Path.Combine(_currentDir, "SPT.Server.exe") },
            { "ip_file", Path.Combine(_currentDir, "ip.txt") }
        };

        if (File.Exists(_paths["ip_file"]))
        {
            Log("ip.txt found");
            _ipFileExists = true;
            try
            {
                Log("calling get_ips()");
                GetIps();
            }
            catch (Exception error)
            {
                Log($"ip.txt error: {error.Message}");
                Console.WriteLine(_strings.File_Error);
                _multiplayerIps = new List<string>();
            }
        }
        else
        {
            Log("ip.txt not found");
            Console.WriteLine(_strings.Ip_Not_Found);
            _ipFileExists = false;
        }
    }

    private void GetIps()
    {
        Log("get_ips() - reading ip.txt");
        var ipList = File.ReadAllLines(_paths["ip_file"])
                         .Select(ip => ip.Trim())
                         .Where(ip => !string.IsNullOrWhiteSpace(ip))
                         .ToList();
        if (ipList.Any())
        {
            Log($"get_ips() - found {ipList.Count} IPs");
            _singlePlayerIp = ipList[0];
            _multiplayerIps = ipList.Skip(1).ToList();
        }
        else
        {
            Log("get_ips() - ip.txt is empty");
            _multiplayerIps = new List<string>();
        }
    }

    private void Log(string msg)
    {
        if (_logEnabled)
        {
            string logMsg = $"[LOG] {msg}";
            _logs.Add(logMsg);
            Console.WriteLine(logMsg);
        }
    }

    private async Task<bool> CheckSingleAttempt(string ip, int port, int timeoutAttempt = 2)
    {
        Log($"cSA() - attempting to connect to {ip}:{port} with timeout {timeoutAttempt}s");
        try
        {
            using var client = new TcpClient();
            var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutAttempt));
            await client.ConnectAsync(ip, port, cancellationTokenSource.Token);
            Log($"cSA() - successfully connected to {ip}:{port}");
            return true;
        }
        catch (Exception ex)
        {
            Log($"cSA() - failed to connect to {ip}:{port} - {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }

    private async Task<string> TryConnection(Uri parsedUrl)
    {
        const int interval = 5;
        const int totalTimeout = 420;

        if (parsedUrl == null || !parsedUrl.IsDefaultPort && parsedUrl.Port == -1)
        {
            Log($"try_connection() - error: parsed_url is invalid: {parsedUrl}");
            return "ParsingError";
        }

        string ip = parsedUrl.DnsSafeHost;
        int port = parsedUrl.Port;
        string fullAddress = $"{ip}:{port}";
        Log($"try_connection() - starting check for {fullAddress}");

        Console.WriteLine(_strings.Server_Timeout_Begin.Replace("{ip}", fullAddress));
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed.TotalSeconds < totalTimeout)
        {
            double elapsed = stopwatch.Elapsed.TotalSeconds;
            Log($"try_connection() - elapsed: {elapsed:F2}s. trying to connect to {fullAddress}");
            Console.WriteLine(_strings.Server_Timeout_Timer
                .Replace("{ip}", fullAddress)
                .Replace("{interval}", (interval + 2).ToString())
                .Replace("{secs}", ((int)elapsed).ToString()));
            
            if (await CheckSingleAttempt(ip, port))
            {
                Log($"try_connection() - server {fullAddress} responded");
                Console.WriteLine(_strings.Server_Started.Replace("{ip}", fullAddress));
                return "True";
            }
            await Task.Delay(TimeSpan.FromSeconds(interval));
        }
        
        Log($"try_connection() - total timeout reached ({totalTimeout}s). Server {fullAddress} did not come online.");
        return "TimeoutHit";
    }

    private string InputPrompt(string promptKey, List<string> validOptions, string retryPromptKey = null)
    {
        Log($"input_prompt() - called with prompt '{promptKey}'");
        retryPromptKey ??= promptKey;
        var errorKey = $"Error_{promptKey}";
        
        Console.Write(GetString(promptKey));
        string value = Console.ReadLine()?.ToUpperInvariant() ?? "";
        
        while (!validOptions.Contains(value))
        {
            Log($"input_prompt() - invalid input '{value}'");
            Console.WriteLine($"{GetString(errorKey)}\n");
            Thread.Sleep(1000);
            Log("input_prompt() - retry prompt called");
            Console.Write(GetString(retryPromptKey));
            value = Console.ReadLine()?.ToUpperInvariant() ?? "";
        }
        Log($"input_prompt() - valid input '{value}'");
        return value;
    }

    public void ShowEndMessage()
    {
        Console.WriteLine(); // newline
        Log("show_end_message() - called");
        Console.WriteLine($"{_strings.EndMsg[_rng[2]]}\n{_strings.Thanks}");
        Console.Write(_strings.Press_Enter);
        Console.ReadLine();
        Environment.Exit(0);
    }

    private void WriteConfigIp(string ip)
    {
        Log($"write_config_ip() - called with ip='{ip}'");
        var configPath = _paths["config"];
        if (!File.Exists(configPath)) return;
        
        Log("write_config_ip() - reading and writing config");
        try
        {
            var lines = File.ReadAllLines(configPath).ToList();
            bool urlWritten = false;
            bool devModeWritten = false;

            for (int i = 0; i < lines.Count; i++)
            {
                var trimmedLine = lines[i].Trim();
                if (!urlWritten && trimmedLine.StartsWith("\"Url\":", StringComparison.OrdinalIgnoreCase))
                {
                    Log("write_config_ip() - found 'Url' line. writing IP");
                    var indent = lines[i].Substring(0, lines[i].IndexOf('"'));
                    lines[i] = $"{indent}\"Url\": \"{ip}\",";
                    urlWritten = true;
                }
                else if (!devModeWritten && trimmedLine.StartsWith("\"IsDevMode\":", StringComparison.OrdinalIgnoreCase))
                {
                    Log("write_config_ip() - found 'IsDevMode' line. writing 'true,'");
                    var indent = lines[i].Substring(0, lines[i].IndexOf('"'));
                    lines[i] = $"{indent}\"IsDevMode\": true,";
                    devModeWritten = true;
                }
            }
            File.WriteAllLines(configPath, lines);
        }
        catch (Exception ex)
        {
            Log($"write_config_ip() - error writing config: {ex.Message}");
        }
    }
    
    public void PerformSetup(string type)
    {
        Log($"PerformSetup() - called for type '{type}'");
        if (type == "sp")
        {
            Log("PerformSetup() - calling RemoveFika() for SP.");
            if (!_dryRun) RemoveFika();
        }
        else if (type == "mpc" || type == "mph")
        {
            Log("PerformSetup() - calling Copy() for MP.");
            if (!_dryRun) Copy();
        }
    }

    public async Task StartProcesses(string type)
    {
        Log($"StartProcesses() - called for type '{type}'");
        if (type == "sp" || type == "mph")
        {
            Log($"StartProcesses() - mode is '{type}', starting server.");
            Console.WriteLine(_strings.Starting_Server);
            Console.WriteLine(_strings.SvMsg.Any() ? _strings.SvMsg[_rng[0]] : "");
            
            if (!_dryRun)
            {
                Log("StartProcesses() - dry_run is false, starting server process.");
                StartProcess(_paths["server"]);
                Log("StartProcesses() - calling TryConnection()");
                string result = await TryConnection(_svip);
                if (result != "True")
                {
                    Log($"StartProcesses() - TryConnection failed with result: {result}");
                    var errorMap = new Dictionary<string, string>
                    {
                        { "ParsingError", _strings.Parsing_Error },
                        { "TimeoutHit", _strings.Server_Timeout_End }
                    };
                    Console.WriteLine(errorMap.GetValueOrDefault(result, _strings.Server_Error_Mystical));
                }
            }
        }

        Log("StartProcesses() - starting launcher.");
        Console.WriteLine(_strings.Starting_Launcher);
        Console.WriteLine(_strings.LMsg.Any() ? _strings.LMsg[_rng[1]] : "");
        if (!_dryRun)
        {
            Log("StartProcesses() - dry_run is false, starting launcher process.");
            StartProcess(_paths["launcher"]);
        }
    }

    private void StartProcess(string filePath, string arguments = "")
    {
        if (File.Exists(filePath))
        {
            try
            {
                Process.Start(new ProcessStartInfo(filePath, arguments)
                {
                    UseShellExecute = true,
                    WorkingDirectory = _currentDir
                });
            }
            catch (Exception ex)
            {
                Log($"Failed to start process '{filePath}': {ex.Message}");
            }
        }
        else
        {
            Log($"File not found: {filePath}");
        }
    }

    public void LauncherIp(string type)
    {
        Log($"launcher_ip() - called for type '{type}'");
        Thread.Sleep(1000);
        Console.WriteLine(_strings.Changing_Ip);
        Thread.Sleep(500);

        if (!File.Exists(_paths["config"]))
        {
            Log("launcher_ip() - config not found");
            Console.WriteLine(_strings.Config_Not_Found.Replace("{path}", _paths["config"]));
            return;
        }

        string targetIp = null;

        if (type == "sp")
        {
            Log("launcher_ip() - type is SP, setting target_ip to singleplayer_ip");
            targetIp = _singlePlayerIp;
        }
        else if (type == "mpc" || type == "mph")
        {
            Log("launcher_ip() - type is MP, determining target_ip");
            if (_args.IpIndex.HasValue)
            {
                Log($"launcher_ip() - --ip-index provided with value {_args.IpIndex.Value}");
                int selectedIndex = _args.IpIndex.Value - 1;
                if (selectedIndex >= 0 && selectedIndex < _multiplayerIps.Count)
                {
                    targetIp = _multiplayerIps[selectedIndex];
                    Log($"launcher_ip() - index is valid, IP selected: {targetIp}");
                    Console.WriteLine(_strings.Ip_Selected.Replace("{num}", _args.IpIndex.Value.ToString()).Replace("{ip}", targetIp));
                }
                else
                {
                    Log("launcher_ip() - index is out of range");
                    Console.WriteLine($"{_strings.Invalid_Ip_Index}\n{_strings.Index_Range.Replace("{range}", _multiplayerIps.Count.ToString())}");
                }
            }
            else if (!_multiplayerIps.Any())
            {
                Log("launcher_ip() - no MP IPs found, defaulting to SP IP");
                Console.WriteLine(_strings.Ip_Missing_Ext);
                targetIp = _singlePlayerIp;
            }
            else if (_multiplayerIps.Count == 1)
            {
                Log("launcher_ip() - only one MP IP found, selecting it automatically");
                targetIp = _multiplayerIps[0];
            }
            else
            {
                Log($"launcher_ip() - {_multiplayerIps.Count} MP IPs found, prompting user");
                Console.WriteLine(_strings.Multiple_Ips);
                Thread.Sleep(1000);
                for (int i = 0; i < _multiplayerIps.Count; i++)
                {
                    Console.WriteLine($"{i + 1} - {_multiplayerIps[i]}");
                    Thread.Sleep(500);
                }
                while (true)
                {
                    try
                    {
                        Console.Write(_strings.Select_Ip);
                        if (int.TryParse(Console.ReadLine(), out int num) && num >= 1 && num <= _multiplayerIps.Count)
                        {
                            targetIp = _multiplayerIps[num - 1];
                            Console.WriteLine(_strings.Ip_Selected.Replace("{num}", num.ToString()).Replace("{ip}", targetIp));
                            Console.WriteLine(); // newline
                            Thread.Sleep(500);
                            break;
                        }
                        else
                        {
                            Console.WriteLine(_strings.Error_Select_Ip_Index);
                            Thread.Sleep(2000);
                            Console.WriteLine(); // newline
                        }
                    }
                    catch (FormatException)
                    {
                        Console.WriteLine(_strings.Error_Select_Ip_Input);
                        Thread.Sleep(2000);
                        Console.WriteLine(); // newline
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(targetIp))
        {
            Log($"launcher_ip() - final target IP set to {targetIp}");
            Uri.TryCreate(targetIp, UriKind.Absolute, out _svip);
            Log($"launcher_ip() - self.svip parsed as: {_svip}");
            if (!_dryRun)
            {
                WriteConfigIp(targetIp);
            }
        }
        else
        {
            Log("launcher_ip() - no target_ip could be determined. svip not set.");
        }
    }

    public void Copy()
    {
        Log("Copy() - called");
        Console.WriteLine(_strings.Deleting_Files);
        try
        {
            Log("Copy() - calling RemoveFika() silently");
            RemoveFika(silent: true);
        }
        catch (Exception error)
        {
            Log($"Copy() - error during silent RemoveFika: {error.Message}");
        }
        Console.WriteLine(_strings.Residues_Removed);
        Thread.Sleep(500);
        Console.WriteLine(_strings.Installing_Fika);

        try
        {
            if (!_dryRun)
            {
                Log("Copy() - dry-run is false, copying files");
                CopyDir(_paths["modFikaUnins"], _paths["modFikaIns"]);
                Directory.CreateDirectory(Path.GetDirectoryName(_paths["pluginFikaIns"]));
                File.Copy(_paths["pluginFikaUnins"], _paths["pluginFikaIns"], true);
            }
            Console.WriteLine($"{_strings.Install_Success}\n");
        }
        catch (Exception erro)
        {
            Log($"Copy() - error during copytree/copy: {erro.Message}");
            Console.WriteLine($"{_strings.Install_Fail}\nError: {erro.Message}");
        }
        Thread.Sleep(1000);
    }
    
    private void CopyDir(string source, string dest)
    {
        if (!Directory.Exists(source)) return;
        Directory.CreateDirectory(dest);
        foreach (var file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
        foreach (var dir in Directory.GetDirectories(source))
            CopyDir(dir, Path.Combine(dest, Path.GetFileName(dir)));
    }

    public void RemoveFika(bool silent = false)
    {
        Log($"RemoveFika() - called (silent={silent})");
        try
        {
            if (!silent) Console.WriteLine(_strings.Uninstalling_Fika);
            
            if (!_dryRun)
            {
                Log("RemoveFika() - dry-run is false, removing files");
                if (File.Exists(_paths["pluginFikaIns"]))
                {
                    File.Delete(_paths["pluginFikaIns"]);
                    Log("RemoveFika() - removed plugin file.");
                }
                if (Directory.Exists(_paths["modFikaIns"]))
                {
                    Directory.Delete(_paths["modFikaIns"], true);
                    Log("RemoveFika() - removed mod directory.");
                }
            }
            Thread.Sleep(1000);
            if (!silent) Console.WriteLine($"{_strings.Uninstall_Success}\n");
        }
        catch (Exception error)
        {
            Log($"RemoveFika() - error: {error.Message}");
            if (!silent) Console.WriteLine(_strings.Uninstall_Fail);
        }
        Thread.Sleep(1000);
    }
    
    public async Task Start()
    {
        Log("start() - interactive mode started");
        Console.WriteLine(_strings.Help_Start_Prompt);
        Thread.Sleep(1500);
        var modeInput = InputPrompt("Start_Prompt", new List<string> { "SP", "MPH", "MPC", "QSP", "QMPH", "QMPC" }, "Retry_Start_Prompt");
        Console.WriteLine(); // newline

        bool isQuick = modeInput.StartsWith("Q");
        string gameMode = isQuick ? modeInput.Substring(1).ToLower() : modeInput.ToLower();
        Log($"start() - mode selected: {gameMode}, quick: {isQuick}");

        LauncherIp(gameMode);

        if (!isQuick)
        {
            Log("start() - normal mode detected, performing setup.");
            PerformSetup(gameMode);
            var autostartInput = InputPrompt("Auto_Start_Prompt", new List<string> { "Y", "N", "FIKOFF" });
            if (autostartInput == "Y")
            {
                Log("start() - user chose to start processes.");
                await StartProcesses(gameMode);
            }
        }
        else
        {
            Log("start() - quick mode detected, skipping setup.");
            Console.WriteLine(_strings.Start_Quick);
            await StartProcesses(gameMode);
        }
        
        ShowEndMessage();
    }

    public static async Task Main(string[] args)
    {
        var opts = ParseArgs(args);
        var main = new FikOff(opts);
        main.Log("main() initialized");
        
        if (!string.IsNullOrEmpty(main._args.LaunchMode))
        {
            string mode = main._args.LaunchMode;
            main.Log($"--launchmode provided. mode: {mode}, quick: {main._args.Quick}, setup: {main._args.Setup}");

            if (!main._args.Quick)
            {
                main.LauncherIp(mode);
            }
            else
            {
                string ipToParse = main._singlePlayerIp;
                if ((mode == "mpc" || mode == "mph") && main._multiplayerIps.Any())
                {
                    int index = main._args.IpIndex.HasValue && main._args.IpIndex.Value -1 >= 0 && main._args.IpIndex.Value - 1 < main._multiplayerIps.Count 
                                ? main._args.IpIndex.Value - 1 
                                : 0;
                    ipToParse = main._multiplayerIps[index];
                }
                Uri.TryCreate(ipToParse, UriKind.Absolute, out main._svip);
                main.Log($"quick mode: svip set to {main._svip}");
            }

            if (main._args.Setup && !main._args.Quick)
            {
                main.Log("--setup flag detected.");
                main.PerformSetup(mode);
            }
            
            main.Log("calling start_processes()");
            await main.StartProcesses(mode);
        }
        else
        {
            main.Log("No --launchmode provided. Defaulting to interactive mode.");
            try
            {
                await main.Start();
            }
            catch(KeyNotFoundException error)
            {
                 main.Log($"configuration error in interactive mode: {error}");
                 Console.WriteLine($"Error loading configuration: {error.Message}");
            }
            catch (Exception error)
            {
                main.Log($"something went to shit in interactive mode: {error}");
            }
        }
    }
    
    private string GetString(string key)
    {
        var property = typeof(Lang).GetProperty(key, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        return property?.GetValue(_strings) as string ?? key;
    }

    private static Options ParseArgs(string[] args)
    {
        var options = new Options();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--launchmode":
                    if (i + 1 < args.Length) options.LaunchMode = args[++i];
                    break;
                case "--quick":
                    options.Quick = true;
                    break;
                case "--setup":
                    options.Setup = true;
                    break;
                case "--ip-index":
                    if (i + 1 < args.Length && int.TryParse(args[++i], out int index))
                    {
                        options.IpIndex = index;
                    }
                    break;
                case "--dry-run":
                    options.DryRun = true;
                    break;
                case "--log":
                    options.Log = true;
                    break;
            }
        }
        return options;
    }
}