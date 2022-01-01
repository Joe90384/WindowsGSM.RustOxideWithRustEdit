using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Query;
using WindowsGSM.Installer;

namespace WindowsGSM.Plugins
{
    public class RustOxideWithRustEdit
    {
        private readonly ServerConfig _serverData;
        public bool AllowsEmbedConsole = false;
        public string FullName = "Rust Dedicated Server with Oxide and RustEdit";


        public string AppId = "258550";
        public string Defaultmap = "Procedural Map";
        public string Maxplayers = "50";
        public string Additional = string.Empty;

        public string Error, Notice;
        
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.RustOxideWithRustEdit",
            author = "Joe 90",
            description = "WindowsGSM plugin for supporting Rust Server with Oxide and RustEdit",
            version = "0.1",
            url = "https://github.com/Joe90384/WindowsGSM-RustOxideWithRustEdit",
            color = "#ef2900" // Color Hex
        };

        public string Port = "28015";
        public int PortIncrements = 1;
        public object QueryMethod = new A2S();
        public string QueryPort = "28015";
        public string StartPath = "RustDedicated.exe";

        public RustOxideWithRustEdit(ServerConfig serverData)
        {
            _serverData = serverData;
        }

        public async void CreateServerCFG()
        {
            var seed = new Random().Next(1, 2147483647);
            var configs = new List<string>
            {
                "+rcon.ip 0.0.0.0",
                $"+rcon.port \"{_serverData.ServerPort}\"",
                $"+rcon.password \"{_serverData.GetRCONPassword()}\"",
                "+rcon.web 1",
                "+server.tickrate 10",
                $"+server.description \"{_serverData.ServerName} - Managed by WindowsGSM\"",
                "+server.url \"\"",
                "+server.headerimage \"\"",
                $"+server.identity \"{_serverData.ServerID}\"",
                $"+server.seed {seed}",
                "+server.worldsize 3000",
                "+server.saveinterval 600",
                "-logfile \"server.log\""
            };
            var config = string.Join("\r\n", configs);
            var configPath = ServerPath.GetServersServerFiles(_serverData.ServerID, "server.cfg");
            File.WriteAllText(configPath, config);
        }

        public async Task<Process> Start()
        {
            await ScheduledWipe();
            var configPath = ServerPath.GetServersServerFiles(_serverData.ServerID, "server.cfg");
            if (!File.Exists(configPath))
                Notice = "server.cfg not found (" + configPath + ")";
            var rustServerPath = ServerPath.GetServersServerFiles(_serverData.ServerID);
            var rustDedicatedServer = Path.Combine(rustServerPath, "RustDedicated.exe");
            var args = new List<string> { "-nographics", "-batchmode", "-silent-crashes" };
            if(!string.IsNullOrWhiteSpace(_serverData.ServerName)) args.Add($"+server.hostname \"{_serverData.ServerName}\"");
            if(!string.IsNullOrWhiteSpace(_serverData.ServerIP)) args.Add($"+server.ip {_serverData.ServerIP}");
            if(!string.IsNullOrWhiteSpace(_serverData.ServerPort)) args.Add($"+server.port {_serverData.ServerPort}");
            if(!string.IsNullOrWhiteSpace(_serverData.ServerMap)) args.Add($"+server.level \"{_serverData.ServerMap}\"");
            if(!string.IsNullOrWhiteSpace(_serverData.ServerMaxPlayer)) args.Add($"+server.maxplayers {_serverData.ServerMaxPlayer}");
            foreach (var config in File.ReadLines(configPath))
                args.Add(config);
            args.Add(_serverData.ServerParam);
            var startupArguments = string.Join(" ", args).Trim();
            var process = new Process();
            process.StartInfo.WorkingDirectory = rustServerPath;
            process.StartInfo.FileName = rustDedicatedServer;
            process.StartInfo.Arguments = startupArguments;
            process.EnableRaisingEvents = true;
            process.Start();
            return process;
        }

        public async Task ScheduledWipe()
        {
            try
            {
                var wipeFile = ServerPath.GetServersConfigs(_serverData.ServerID, "auto_wipe.cfg");
                if (!File.Exists(wipeFile))
                    return;
                var schedules =
                    JsonConvert.DeserializeObject<Dictionary<string, WipeSchedule>>(File.ReadAllText(wipeFile));

                await CheckWipeSchedules(schedules);
                CalculateNextWipes(schedules, wipeFile);
            }
            catch (Exception e)
            {
                var autoWipeLog = ServerPath.GetServersServerFiles(_serverData.ServerID, "auto_wipe.log");
                using (var logWriter = new StreamWriter(autoWipeLog, true))
                {
                    await logWriter.WriteLineAsync("".PadRight(100, '-'));
                    await logWriter.WriteLineAsync($"Invalid wipe file: {e.Message}");
                    await logWriter.WriteLineAsync("".PadRight(100, '-'));
                }
            }
        }

        private async Task CheckWipeSchedules(Dictionary<string, WipeSchedule> schedules)
        {
            foreach (var kvp in schedules)
            {
                if (kvp.Value.NextWipe <= DateTime.Now)
                {
                    await WipeServerFiles(kvp.Key, kvp.Value);
                }
            }
        }

        private async Task WipeServerFiles(string name, WipeSchedule schedule)
        {
            var serverFileDirectoryInfo =
                new DirectoryInfo(ServerPath.GetServersServerFiles(_serverData.ServerID, "server", _serverData.ServerID));
            var autoWipeLog = ServerPath.GetServersServerFiles(_serverData.ServerID, "auto_wipe.log");
            using (var logWriter = new StreamWriter(autoWipeLog, true))
            {
                await logWriter.WriteLineAsync("".PadRight(100, '-'));
                await logWriter.WriteLineAsync($"Auto Wipe Starting for: {name} ({DateTime.Now})");
                foreach (var searchPattern in schedule.Files)
                {
                    await logWriter.WriteLineAsync($"Search Pattern: {searchPattern}");
                    var files = serverFileDirectoryInfo.GetFiles(searchPattern);
                    foreach (var file in files)
                    {
                        await logWriter.WriteLineAsync($"Deleted: {file.Name}");
                        file.Delete();
                    }
                }

                await logWriter.WriteLineAsync("".PadRight(100, '-'));
            }
        }

        private static void CalculateNextWipes(Dictionary<string, WipeSchedule> schedules, string wipeFile)
        {
            var today = DateTime.Now;
            foreach (var schedule in schedules.Values)
            {
                for (var index = 0; index < schedule.Weeks.Length; index++)
                {
                    var week = schedule.Weeks[index] - 1;
                    var day = 7 * week + 1;
                    if (day < 1) day = 1;
                    var wipeDate = new DateTime(today.Year, today.Month, day, 0, 0, 0);
                    while (wipeDate.DayOfWeek != DayOfWeek.Sunday)
                        wipeDate = wipeDate.AddDays(1);
                    while (schedule.Day != wipeDate.DayOfWeek)
                        wipeDate = wipeDate.AddDays(1);
                    if (wipeDate.Month != today.Month)
                    {
                        index = -1;
                        today = today.AddMonths(1);
                        continue;
                    }

                    wipeDate += schedule.Time;
                    if (DateTime.Now < wipeDate)
                    {
                        schedule.NextWipe = wipeDate;
                        break;
                    }
                }
            }

            File.WriteAllText(wipeFile, JsonConvert.SerializeObject(schedules, Formatting.Indented));
        }
        public class WipeSchedule
        {
            [JsonProperty("weeks")]
            public int[] Weeks { get; set; }
            [JsonProperty("day")]
            public DayOfWeek Day { get; set; }
            [JsonProperty("time")]
            public TimeSpan Time { get; set; }
            [JsonProperty("files")]
            public string[] Files { get; set; }
            [JsonProperty("next")]
            public DateTime NextWipe { get; set; }
        }

        public async Task Stop(Process p)
        {
            await Task.Run(() => ServerConsole.SendMessageToMainWindow(p.MainWindowHandle, "quit"));
        }

        public async Task<Process> Install()
        {
            var steamCMD = new SteamCMD();
            var process = await steamCMD.Install(_serverData.ServerID, string.Empty, AppId);
            Error = steamCMD.Error;
            await InstallOxideAndRustEdit();
            WriteWipeConfigTemplate();
            return process;
        }

        public void WriteWipeConfigTemplate()
        {
            var wipeTemplateFile = ServerPath.GetServersConfigs(_serverData.ServerID, "auto_wipe.template.cfg");
            var wipeTemplateContent = @"{
  ""Map Wipe"": {                                       // Name for this wipe schedule (must be unique)
    ""weeks"": [2,4],                                   // Wipe on 2nd and 4th weeks
    ""day"": 4,                                         // Wipe on Thursday (0 - 6 => Sunday - Saturday)
    ""time"": ""12:00"",                                // Wipe at next restart after 12:00 (midday server time)
    ""files"": [""*.sav""]                              // Wipe map only
  },
  ""BP Wipe"": {                                        // Name for this wipe schedule (must be unique)
    ""weeks"": [1,3,5],                                 // Wipe on 1st, 3rd and 5th weeks
    ""day"": 4,                                         // Wipe on Thursday
    ""time"": ""12:00"",                                // Wipe at next restart after 12:00 (midday server time)
    ""files"": [""*.db"",""*.sav"",""*.db-journal""]    // Wipe map and blueprints
  }
}";
            File.WriteAllText(wipeTemplateFile, wipeTemplateContent);
        }

        public async Task<Process> Update(bool validate = false, string custom = null)
        {
            Process process;
            (process, Error) =
                await SteamCMD.UpdateEx(_serverData.ServerID, AppId, validate, custom: custom);

            await InstallOxideAndRustEdit();


            return process;
        }

        private async Task InstallOxideAndRustEdit()
        {
            try
            {
                var rustServer = ServerPath.GetServersServerFiles(_serverData.ServerID);
                using (var stream = new StreamWriter(Path.Combine(rustServer, "gsm_update.log"), true))
                {
                    await stream.WriteLineAsync($" {DateTime.Now} ".PadRight(25, '-').PadLeft(50, '-'));
                    await InstallOxide(rustServer, stream);
                    await InstallRustEdit(rustServer, stream);
                    await stream.WriteLineAsync("".PadRight(50, '-'));
                }
            }
            catch (Exception e)
            {
                Error = e.Message;
            }
        }

        private static async Task InstallRustEdit(string rustServer, StreamWriter stream)
        {
            var rustEditZipFiles = Path.Combine(rustServer, "Oxide.Ext.RustEdit.zip");
            if (File.Exists(rustEditZipFiles))
                File.Delete(rustEditZipFiles);
            using (var client = new WebClient())
            {
                client.DownloadFile(
                    new Uri("https://github.com/k1lly0u/Oxide.Ext.RustEdit/archive/refs/heads/master.zip"),
                    rustEditZipFiles);
            }

            var rustEditServerFilesPath = Path.Combine(rustServer, "Oxide.Ext.RustEdit");
            if (Directory.Exists(rustEditServerFilesPath))
                Directory.Delete(rustEditServerFilesPath, true);
            await FileManagement.ExtractZip(rustEditZipFiles, rustEditServerFilesPath);
            var rustEditSource = new FileInfo(Path.Combine(rustEditServerFilesPath, "Oxide.Ext.RustEdit-master",
                "Oxide.Ext.RustEdit.dll"));
            var rustEditDestination = new FileInfo(Path.Combine(rustServer, "RustDedicated_Data", "Managed",
                "Oxide.Ext.RustEdit.dll"));

            if (File.Exists(rustEditSource.FullName) && rustEditDestination.Directory.Exists)
            {
                if (!FilesAreEqual_Hash(rustEditSource, rustEditDestination))
                    try
                    {
                        rustEditSource.CopyTo(rustEditDestination.FullName);
                        await stream.WriteLineAsync($"{rustEditSource.FullName} => {rustEditDestination.FullName}");
                    }
                    catch (Exception e)
                    {
                        await stream.WriteLineAsync($"ERROR: {e.Message}");
                    }
                else
                    await stream.WriteLineAsync($"Skipped (same): {rustEditSource.Name}");
            }
            else
            {
                await stream.WriteLineAsync("ERROR: RustEdit Source or Destination missing");
            }
        }

        private static async Task InstallOxide(string rustServer, StreamWriter stream)
        {
            var oxideServerFilesZipPath = Path.Combine(rustServer, "Oxide.Rust.zip");
            if (File.Exists(oxideServerFilesZipPath))
                File.Delete(oxideServerFilesZipPath);
            using (var client = new WebClient())
            {
                client.DownloadFile(new Uri("https://umod.org/games/rust/download?tag=public"),
                    oxideServerFilesZipPath);
            }

            var oxideServerFilesPath = Path.Combine(rustServer, "Oxide.Rust");
            if (Directory.Exists(oxideServerFilesPath))
                Directory.Delete(oxideServerFilesPath, true);
            await FileManagement.ExtractZip(oxideServerFilesZipPath, oxideServerFilesPath);
            var oxideDedicatedDataPath = Path.Combine(oxideServerFilesPath, "RustDedicated_Data");
            var oxideDedicatedData = new DirectoryInfo(oxideDedicatedDataPath);
            var rustDedicatedDataPath = Path.Combine(rustServer, "RustDedicated_Data");
            var managedDataFiles = oxideDedicatedData.GetFiles("*.*", SearchOption.AllDirectories);
            await stream.WriteLineAsync($"Oxide Update: {DateTime.Now}");
            await stream.WriteLineAsync(oxideServerFilesZipPath);

            foreach (var sourceFileInfo in managedDataFiles)
            {
                var destinationFileInfo =
                    new FileInfo(sourceFileInfo.FullName.Replace(oxideDedicatedDataPath, rustDedicatedDataPath));
                try
                {
                    if (!destinationFileInfo.Directory.Exists)
                    {
                        destinationFileInfo.Directory.Create();
                    }
                    else if (destinationFileInfo.Exists && FilesAreEqual_Hash(sourceFileInfo, destinationFileInfo))
                    {
                        await stream.WriteLineAsync($"Skipped (same): {sourceFileInfo.Name}");
                        continue;
                    }

                    File.Copy(sourceFileInfo.FullName, destinationFileInfo.FullName, true);
                }
                catch (Exception e)
                {
                    await stream.WriteLineAsync($"ERROR: {e.Message}");
                }

                await stream.WriteLineAsync($"{sourceFileInfo.FullName} => {destinationFileInfo}");
            }
        }

        private static bool FilesAreEqual_Hash(FileInfo first, FileInfo second)
        {
            if (!first.Exists || !second.Exists)
                return false;
            var firstHash = MD5.Create().ComputeHash(first.OpenRead());
            var secondHash = MD5.Create().ComputeHash(second.OpenRead());

            for (var i = 0; i < firstHash.Length; i++)
                if (firstHash[i] != secondHash[i])
                    return false;
            return true;
        }

        public bool IsInstallValid()
        {
            return File.Exists(ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath));
        }

        public bool IsImportValid(string path)
        {
            Error = "Invalid Path! Fail to find " + StartPath;
            return File.Exists(Path.Combine(path, StartPath));
        }

        public string GetLocalBuild()
        {
            return new SteamCMD().GetLocalBuild(_serverData.ServerID, AppId);
        }

        public async Task<string> GetRemoteBuild()
        {
            return await new SteamCMD().GetRemoteBuild(AppId);
        }
    }
}