#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;

public class Lang
{
    public List<string> EndMsg { get; set; } = new List<string>();
    public List<string> LMsg { get; set; } = new List<string>();
    public List<string> SvMsg { get; set; } = new List<string>();
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
    public string Ip_Missing_Ext { get; set; } = "";
    public string Ip_Selected { get; set; } = "";
    public string Multiple_Ips { get; set; } = "";
    public string Index_Missing { get; set; } = "";
    public string Index_Range { get; set; } = "";
    public string Invalid_Ip_Index { get; set; } = "";
    public string Starting_Server { get; set; } = "";
    public string Starting_Launcher { get; set; } = "";
    public string Changing_Ip { get; set; } = "";
    public string Config_Not_Found { get; set; } = "";
    public string Deleting_Files { get; set; } = "";
    public string Residues_Removed { get; set; } = "";
    public string Installing_Fika { get; set; } = "";
    public string Install_Success { get; set; } = "";
    public string Install_Fail { get; set; } = "";
    public string Uninstalling_Fika { get; set; } = "";
    public string Uninstall_Success { get; set; } = "";
    public string Thanks { get; set; } = "";
    public string Press_Enter { get; set; } = "";
    public string Start_Quick { get; set; } = "";
}

public class Options
{
    public string LaunchMode { get; set; }
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
            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var langDataRaw = File.ReadAllText(langPath, Encoding.UTF8);
            var langDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(langDataRaw, jsonOptions);

            string selectedLangKey = "en";
            if (langDict.TryGetValue("selected", out var selectedLangElement))
            {
                selectedLangKey = selectedLangElement.GetString() ?? "en";
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

        if (_args.LaunchMode != "quick")
        {
            InitializeIps();
        }
    }

    private void InitializeIps()
    {
        var ipFilePath = _paths["ip_file"];
        if (File.Exists(ipFilePath))
        {
            _ipFileExists = true;
            try
            {
                var ipList = File.ReadAllLines(ipFilePath)
                                 .Select(ip => ip.Trim())
                                 .Where(ip => !string.IsNullOrWhiteSpace(ip))
                                 .ToList();
                if (ipList.Any())
                {
                    Console.WriteLine(_strings.Ip_Found);
                    _singlePlayerIp = ipList[0];
                    _multiplayerIps = ipList.Skip(1).ToList();
                }
                else
                {
                    Console.WriteLine(_strings.Ip_Not_Found);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{_strings.File_Error}: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine(_strings.Ip_Not_Found);
            _ipFileExists = false;
        }
    }

    private string GetString(string key)
    {
        var property = typeof(Lang).GetProperty(key, System.Reflection.BindingFlags.IgnoreCase | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        return property?.GetValue(_strings) as string ?? key;
    }

    private string InputPrompt(string promptKey, List<string> validOptions, string retryPromptKey = null)
    {
        retryPromptKey ??= promptKey;
        var errorKey = $"Error_{promptKey}";
        
        Console.Write(GetString(promptKey));
        string value = Console.ReadLine()?.ToUpperInvariant() ?? "";
        
        while (!validOptions.Contains(value))
        {
            Console.WriteLine(GetString(errorKey));
            Thread.Sleep(1000);
            
            Console.Write(GetString(retryPromptKey));
            value = Console.ReadLine()?.ToUpperInvariant() ?? "";
        }
        return value;
    }

    public void ShowEndMessage()
    {
        Console.WriteLine($"{_strings.EndMsg[_rng[2]]}\n{_strings.Thanks}");
        Console.Write(_strings.Press_Enter);
        Console.ReadLine();
        Environment.Exit(0);
    }

    private void WriteConfigIp(string ip)
    {
        var configPath = _paths["config"];
        if (!File.Exists(configPath)) return;

        try
        {
            var lines = File.ReadAllLines(configPath).ToList();
            for (int i = 0; i < lines.Count; i++)
            {
                var trimmedLine = lines[i].Trim();
                if (trimmedLine.StartsWith("\"Url\":", StringComparison.OrdinalIgnoreCase))
                {
                    var indent = lines[i].Substring(0, lines[i].IndexOf('"'));
                    lines[i] = $"{indent}\"Url\": \"{ip}\",";
                    break;
                }
            }
            File.WriteAllLines(configPath, lines);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to write to config file: {ex.Message}");
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
                Console.WriteLine($"Failed to start process '{filePath}': {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine($"File not found: {filePath}");
        }
    }

    public void LaunchSequence(string type)
    {
        Console.WriteLine();
        
        if (type == "quick")
        {
            Console.WriteLine(_strings.Start_Quick);
            if (!_dryRun)
            {
                StartProcess(_paths["server"]);
                StartProcess(_paths["launcher"]);
            }
            return;
        }

        LauncherIp(type);

        if (type == "sp")
        {
            Console.WriteLine(_strings.Starting_Server);
            Console.WriteLine(_strings.SvMsg.Any() ? _strings.SvMsg[_rng[0]] : "");
            if (!_dryRun)
            {
                StartProcess(_paths["server"]);
                Thread.Sleep(10000);
            }
        }

        Console.WriteLine(_strings.Starting_Launcher);
        Console.WriteLine(_strings.LMsg.Any() ? _strings.LMsg[_rng[1]] : "");
        if (!_dryRun)
        {
            StartProcess(_paths["launcher"]);
        }
    }

    private void LauncherIp(string type)
    {
        Thread.Sleep(1000);
        Console.WriteLine(_strings.Changing_Ip);
        Thread.Sleep(500);

        if (!File.Exists(_paths["config"]))
        {
            Console.WriteLine(_strings.Config_Not_Found.Replace("{path}", _paths["config"]));
            return;
        }

        string targetIp = (type == "sp") ? _singlePlayerIp : null;

        if (type == "mp")
        {
            if (_args.IpIndex.HasValue)
            {
                int selectedIndex = _args.IpIndex.Value - 1;
                if (selectedIndex >= 0 && selectedIndex < _multiplayerIps.Count)
                {
                    targetIp = _multiplayerIps[selectedIndex];
                    Console.WriteLine(_strings.Ip_Selected.Replace("{num}", _args.IpIndex.ToString()).Replace("{ip}", targetIp));
                }
                else
                {
                    Console.WriteLine($"{_strings.Invalid_Ip_Index}\n{_strings.Index_Range.Replace("{range}", _multiplayerIps.Count.ToString())}\n{_strings.Ip_Missing_Ext}");
                    targetIp = _singlePlayerIp;
                }
            }
            else if (_multiplayerIps.Count == 1)
            {
                targetIp = _multiplayerIps[0];
            }
            else if (!_multiplayerIps.Any())
            {
                targetIp = _singlePlayerIp;
                Console.WriteLine(_strings.Ip_Missing_Ext);
            }
            else
            {
                Console.WriteLine(_strings.Multiple_Ips);
                for (int i = 0; i < _multiplayerIps.Count; i++)
                {
                    Console.WriteLine($"{i + 1} - {_multiplayerIps[i]}");
                }

                while (true)
                {
                    Console.Write(_strings.Select_Ip);
                    if (int.TryParse(Console.ReadLine(), out int num) && num >= 1 && num <= _multiplayerIps.Count)
                    {
                        targetIp = _multiplayerIps[num - 1];
                        Console.WriteLine(_strings.Ip_Selected.Replace("{num}", num.ToString()).Replace("{ip}", targetIp));
                        break;
                    }
                    else
                    {
                        Console.WriteLine(_strings.Error_Select_Ip_Index);
                    }
                }
            }
        }
        
        if (targetIp != null)
        {
            WriteConfigIp(targetIp);
        }
    }

    public void CopyFika()
    {
        Console.WriteLine($"\n{_strings.Deleting_Files}");
        try { RemoveFika(silent: true); } catch { /* ignored */ }

        Console.WriteLine(_strings.Residues_Removed);
        Console.WriteLine($"\n{_strings.Installing_Fika}");

        try
        {
            if (!_dryRun)
            {
                void CopyDir(string source, string dest)
                {
                    Directory.CreateDirectory(dest);
                    foreach (var file in Directory.GetFiles(source))
                        File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
                    foreach (var dir in Directory.GetDirectories(source))
                        CopyDir(dir, Path.Combine(dest, Path.GetFileName(dir)));
                }

                if (Directory.Exists(_paths["modFikaUnins"]))
                {
                    CopyDir(_paths["modFikaUnins"], _paths["modFikaIns"]);
                }

                string pluginDestDir = Path.GetDirectoryName(_paths["pluginFikaIns"]);
                if (!string.IsNullOrEmpty(pluginDestDir))
                {
                    Directory.CreateDirectory(pluginDestDir);
                    if (File.Exists(_paths["pluginFikaUnins"]))
                    {
                        File.Copy(_paths["pluginFikaUnins"], _paths["pluginFikaIns"], true);
                    }
                }
            }
            Console.WriteLine($"{_strings.Install_Success}\n");
        }
        catch (Exception erro)
        {
            Console.WriteLine($"{_strings.Install_Fail}\nError: {erro.Message}");
        }
    }

    public void RemoveFika(bool silent = false)
    {
        if (!silent) Console.WriteLine($"\n{_strings.Uninstalling_Fika}");

        if (!_dryRun)
        {
            try
            {
                if (File.Exists(_paths["pluginFikaIns"])) File.Delete(_paths["pluginFikaIns"]);
                if (Directory.Exists(_paths["modFikaIns"])) Directory.Delete(_paths["modFikaIns"], true);
            }
            catch (Exception ex)
            {
                if (!silent) Console.WriteLine($"Error removing FIKA: {ex.Message}");
            }
        }

        if (!silent) Console.WriteLine($"{_strings.Uninstall_Success}\n");
    }

    public void Start()
    {
        var mode = InputPrompt("Start_Prompt", new List<string> { "SP", "MP", "START" }, "Retry_Start_Prompt");

        switch (mode)
        {
            case "START":
                LaunchSequence("quick");
                break;
            case "MP":
                if (_ipFileExists && _multiplayerIps.Any())
                {
                    CopyFika();
                    var start = InputPrompt("Auto_Start_Prompt", new List<string> { "Y", "N" });
                    if (start == "Y") LaunchSequence("mp");
                    ShowEndMessage();
                }
                else
                {
                    Console.WriteLine(_strings.Ip_Missing);
                    Console.WriteLine(_strings.Ip_Missing_Ext);
                    LaunchSequence("sp");
                }
                break;
            case "SP":
                try { RemoveFika(); } catch { /* ignored */ }
                var startSp = InputPrompt("Auto_Start_Prompt", new List<string> { "Y", "N" });
                if (startSp == "Y") LaunchSequence("sp");
                ShowEndMessage();
                break;
        }
    }

    public static void Main(string[] args)
    {
        try
        {
            var opts = ParseArgs(args);
            var fikoff = new FikOff(opts);
            if (!string.IsNullOrEmpty(fikoff._args.LaunchMode))
            {
                switch (fikoff._args.LaunchMode)
                {
                    case "quick":
                        fikoff.LaunchSequence("quick");
                        break;
                    case "sp":
                        if (fikoff._args.Setup) try { fikoff.RemoveFika(true); } catch { /* ignored */ }
                        fikoff.LaunchSequence("sp");
                        break;
                    case "mp":
                        if (fikoff._args.Setup) fikoff.CopyFika();
                        fikoff.LaunchSequence("mp");
                        break;
                }
            }
            else
            {
                fikoff.Start();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("\nCaught an unhandled exception. Maybe you forgot to download the lang.json file or it's in the wrong place?");
            Console.WriteLine($"Error details: {e.Message}");
            Console.WriteLine("Press Enter to exit.");
            Console.ReadLine();
        }
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