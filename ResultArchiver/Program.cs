using Newtonsoft.Json;
using ResultArchiver.Classes;
using ResultArchiver.Settings;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Serilog;

namespace ResultArchiver
{
    internal class Program
    {
        private static FileSystemWatcher _watcher;
        private static SettingsJDO _settings { get; set; } = new SettingsJDO();

        private static ILogger Logger = new LoggerConfiguration()
                .WriteTo.File(path: Constants.LOG_PATH, retainedFileCountLimit: 10, rollingInterval: RollingInterval.Day)
                .CreateLogger();

        #region user32.dll import

        private const int MF_BYCOMMAND = 0x00000000;
        public const int SC_CLOSE = 0xF060;

        [DllImport("user32.dll")]
        public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        #endregion

        static void Main(string[] args)
        {
            bool CloseApp = false;

            DisableCloseAppFunction();

            Console.Title = Constants.APPLICATION_NAME;

            ShowInfo();

            _settings = ReadSettingsJSON(Constants.SETTINGS_PATH);

            CheckSettings(_settings);

            ShowFileFilterInformation(_settings);

            _watcher = CreateWatcher(_settings.FileFoldeCheckerSettings);

            while (CloseApp == false)
            {
                var ConsoleInput = Console.ReadLine();

                if (ConsoleInput != null)
                {
                    if (ConsoleInput.ToUpper() == "STOP")
                    {
                        CloseApp = true;
                    }
                }
            }
        }

        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            foreach (string item in _settings.FileIgnoreFilter)
            {
                if ((item + _settings.FileFoldeCheckerSettings.Extension) == e.Name)
                {
                    return;
                }
            }

            ConsoleWriteLine("", default, false);
            ConsoleWriteLine("New File Created: " + e.FullPath, ConsoleColor.Blue);

            string destinationPath = Path.ChangeExtension(_settings.DestinationPath + @"\\" + Path.GetFileName(e.FullPath), ".zip");

            DeleteOldestFileIfAmountBiggerThenSet(_settings);

            Thread.Sleep(1000);

            if (CheckIfDriveFreeSpaceForFileIsOK(_settings, e.FullPath))
            {
                if (File.Exists(destinationPath))
                {
                    ConsoleWriteLine("Archive file already exist. Path: " + destinationPath, ConsoleColor.DarkYellow);
                    ConsoleWriteLine("Archiving file is skipped.", ConsoleColor.DarkYellow);

                    if (_settings.DeleteResultAfterArchivate)
                    {
                        DeleteFile(e.FullPath);
                    }
                }
                else
                {
                    bool archiveFileError = ArchiveFile(e, destinationPath, _settings.CompressionLevel);

                    if (_settings.DeleteResultAfterArchivate && archiveFileError == false)
                    {
                        DeleteOriginalFileFile(e, destinationPath);
                    }
                }
            }
        }

        private static void ShowFileFilterInformation(SettingsJDO settings)
        {
            int fileIgnoreFilterCount = settings.FileIgnoreFilter.Count;

            if (fileIgnoreFilterCount > 0)
            {
                ConsoleWriteLine("", default, false);
                ConsoleWriteLine("File filter is Active. Amount of items in filter is: " + fileIgnoreFilterCount.ToString(), ConsoleColor.Blue);

                foreach (string item in settings.FileIgnoreFilter)
                {
                    ConsoleWriteLine("No operations will be performed on the following file. File Name: " + item.ToString(), ConsoleColor.Blue);
                }
            }
        }

        private static bool CheckIfDriveFreeSpaceForFileIsOK(SettingsJDO settings, string filePath)
        {
            long driveFreeSpace = 0;

            ConsoleWriteLine("Check if drive exist", ConsoleColor.Blue);

            if (Directory.Exists(settings.DestinationPath)) 
            {
                ConsoleWriteLine("Drive exist", ConsoleColor.Green);

                string pathRoot = Path.GetPathRoot(_settings.DestinationPath)!;

                ConsoleWriteLine("Getting free space on drive. Drive: " + pathRoot, ConsoleColor.Blue);

                driveFreeSpace = GetTotalDriveFreeSpace(pathRoot);

                ConsoleWriteLine("Free space on drive is: " + SizeSuffix(driveFreeSpace, 2), ConsoleColor.Green);
                ConsoleWriteLine("Getting new file size.", ConsoleColor.Blue);

                long fileSize = GetFileSize(filePath);
                long minFreeSpaceSize = fileSize + 5368709120;

                ConsoleWriteLine("New file size is: " + SizeSuffix(fileSize, 2), ConsoleColor.Green);
                ConsoleWriteLine("Min free space on drive is: " + SizeSuffix(minFreeSpaceSize, 2), ConsoleColor.Green);
                ConsoleWriteLine("Checking if free space is sufficient for new file.", ConsoleColor.Blue);

                if (driveFreeSpace >= minFreeSpaceSize) 
                {
                    ConsoleWriteLine("Free space is sufficient for new file.", ConsoleColor.Green);
                    return true;
                }
                else
                {
                    ConsoleWriteLine("Free space is NOT sufficient for new file.", ConsoleColor.DarkYellow);
                    ConsoleWriteLine("All next action is skipped", ConsoleColor.Blue);
                }
            }
            else
            {
                ConsoleWriteLine("Drive NOT exist", ConsoleColor.Red);
            }

            return false;
        }

        private static string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} bytes", 0); }

            int mag = (int)Math.Log(value, 1024);
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                Constants.SIZE_SUFFIXIES[mag]);
        }

        private static long GetTotalDriveFreeSpace(string driveName)
        {
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (drive.IsReady && drive.Name == driveName)
                {
                    return drive.TotalFreeSpace;
                }
            }
            return -1;
        }

        private static void DeleteOldestFileIfAmountBiggerThenSet(SettingsJDO settings)
        {
            if (Directory.Exists(settings.DestinationPath))
            {
                ConsoleWriteLine("", default, false);
                ConsoleWriteLine("Checking amounth of file in destination folder", ConsoleColor.Blue);

                int amountOfFile = CheckAmountOfArchiveFile(settings.DestinationPath);

                ConsoleWriteLine("Amounth of file is: " + amountOfFile.ToString(), ConsoleColor.Blue);

                if (amountOfFile >= settings.MaxAmountOfArchiveFileInFolder)
                {
                    ConsoleWriteLine("Amounth of file is bigger or equal then set. Set is: " + settings.MaxAmountOfArchiveFileInFolder.ToString(), ConsoleColor.Blue);
                    ConsoleWriteLine("Deleting all file above set value. Set is: " + settings.MaxAmountOfArchiveFileInFolder.ToString(), ConsoleColor.Blue);

                    for (int i = settings.MaxAmountOfArchiveFileInFolder - 1; i < amountOfFile; i++)
                    {
                        ConsoleWriteLine("Getting oldest file path.", ConsoleColor.Blue);

                        string oldestFilePath = GetOldestFileFromDirectory(".zip", settings.DestinationPath);

                        ConsoleWriteLine("Oldest file path is: " + oldestFilePath, ConsoleColor.Green);
                        ConsoleWriteLine("Deleting oldest file.", ConsoleColor.Blue);

                        if (File.Exists(oldestFilePath))
                        {
                            File.Delete(oldestFilePath);
                            ConsoleWriteLine("Oldest file deleted.", ConsoleColor.Green);
                        }
                        else
                        {
                            ConsoleWriteLine("Oldest file deleting ERROR: File not exist.", ConsoleColor.Red);
                        }
                    }
                }

                ConsoleWriteLine("", default, false);
            }
        }

        private static long GetFileSize(string path)
        {
            return new FileInfo(path).Length;
        }

        private static int CheckAmountOfArchiveFile(string path)
        {
            try
            {
                string[] files = files = Directory.GetFiles(path, "*.zip");
                return files.Length;
            }
            catch
            {
                return 0;
            }
        }

        private static string GetOldestFileFromDirectory(string extension, string path)
        {
            return new DirectoryInfo(path).GetFiles("*" + extension).MinBy(o => o.CreationTime)!.FullName;
        }

        private static void ConsoleWriteLine(string text, ConsoleColor textColor, bool addPrefixTime = true)
        {
            Console.ForegroundColor = textColor;

            switch (textColor)
            {
                case ConsoleColor.DarkRed:
                    Logger.Error(text);
                    break;
                case ConsoleColor.DarkYellow:
                    Logger.Warning(text);
                    break;
                case ConsoleColor.Red:
                    Logger.Error(text);
                    break;
                case ConsoleColor.Yellow:
                    Logger.Warning(text);
                    break;
                default:
                    if (_settings.EnableLoging)
                    {
                        Logger.Information(text);
                    }
                    break;
            }

            string tmp_text;

            if (addPrefixTime)
            {
                DateTime dateTime = DateTime.Now;
                tmp_text = $"<{dateTime.ToString("G")}> {text}";
            }
            else
            {
                tmp_text = text;
            }

            Console.WriteLine(tmp_text);

            Console.ForegroundColor = ConsoleColor.White;
        }

        private static void DisableCloseAppFunction()
        {
            DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_CLOSE, MF_BYCOMMAND);

            // Disable CTRL + C
            Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e) {
                e.Cancel = true;
            };
        }

        private static void DeleteOriginalFileFile(FileSystemEventArgs e, string destinationPath)
        {
            try
            {
                ConsoleWriteLine("Check if new archive exist.", ConsoleColor.Blue);

                if (File.Exists(destinationPath))
                {
                    ConsoleWriteLine("New archive exist. Deleting original file. Path: " + e.FullPath, ConsoleColor.Blue);
                    File.Delete(e.FullPath);
                }

                ConsoleWriteLine("Deleting DONE.", ConsoleColor.Green);

            }
            catch (Exception ex)
            {
                ConsoleWriteLine("Error while checking or deleting file: " + ex.Message, ConsoleColor.Red);
            }
        }

        private static void DeleteFile(string path)
        {
            try 
            {
                ConsoleWriteLine("Deleting duplicit file. Path: " + path, ConsoleColor.Blue);

                File.Delete(path);

                ConsoleWriteLine("Deleting duplicit file DONE.", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                ConsoleWriteLine("Deleting duplicit file ERROR: " + ex.Message, ConsoleColor.Red);
            }
        }

        private static bool ArchiveFile(FileSystemEventArgs e, string destinationPath, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            bool error = false;

            try
            {
                ConsoleWriteLine("Archivate file: " + e.Name, ConsoleColor.Blue);

                using (ZipArchive archive = ZipFile.Open(destinationPath, ZipArchiveMode.Create))
                {
                    archive.CreateEntryFromFile(e.FullPath, Path.GetFileName(e.FullPath), compressionLevel);
                }

                ConsoleWriteLine("Archiving DONE. Archive Path: " + destinationPath, ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                error = true;
                ConsoleWriteLine("Archiving FAILED. Error: " + ex.Message, ConsoleColor.Red);
            }

            return error;
        }

        private static void ShowInfo()
        {
            ConsoleWriteLine(Constants.CONSOLE_APP_NAME_TEXT, ConsoleColor.DarkMagenta, false);
            ConsoleWriteLine(Constants.ABOUT_APP_TABLE, ConsoleColor.DarkMagenta, false);
            ConsoleWriteLine("", default, false);
            ConsoleWriteLine("Result Archiver Application STARTED.", ConsoleColor.Blue);
            ConsoleWriteLine("", default, false);
        }

        private static FileSystemWatcher CreateWatcher(FileFoldeCheckerSettingsJDO settings)
        {
            ConsoleWriteLine("", default, false);
            ConsoleWriteLine("Setuping File/Folder Change Watcher", ConsoleColor.Blue);

            try
            {
                var watcher = new FileSystemWatcher(settings.Path);

                watcher.NotifyFilter = NotifyFilters.FileName;

                watcher.Created += OnCreated;
                watcher.Error += Watcher_Error;

                watcher.Filter = "*" + settings.Extension;
                watcher.IncludeSubdirectories = settings.IncludeSubdirectories;
                watcher.EnableRaisingEvents = true;

                ConsoleWriteLine("Setuping File/Folder Change Watcher DONE", ConsoleColor.Green);

                return watcher;
            }
            catch (Exception ex)
            {
                ConsoleWriteLine("Setuping File/Folder Change Watcher ERROR: " + ex.Message, ConsoleColor.Red);
                return new FileSystemWatcher();
            }
        }

        private static void Watcher_Error(object sender, ErrorEventArgs e)
        {
            ConsoleWriteLine("", default, false);
            ConsoleWriteLine("Watcher ERROR: " + e.GetException().Message, ConsoleColor.Red);
        }

        private static string GetApplicationFolder()
        {
            string fullPath = System.Reflection.Assembly.GetExecutingAssembly().Location;

            if (fullPath is null)
            {
                return "";
            }
            else
            {
                return Path.GetDirectoryName(fullPath)!;
            }
        }

        private static void CheckSettings(SettingsJDO settings)
        {
            ConsoleWriteLine("Check Configuration", ConsoleColor.Blue);

            bool error = false;

            if (Directory.Exists(settings!.FileFoldeCheckerSettings.Path) == false)
            {
                error = true;
            }

            if (Directory.Exists(settings!.DestinationPath) == false)
            {
                error = true;
            }

            if (settings.FileFoldeCheckerSettings.Extension == "")
            {
                error = true;
            }

            if (error)
            {
                ConsoleWriteLine("CONFIG ERROR. CLOSING APPLICATION", ConsoleColor.Red);
                Thread.Sleep(5000);
                Environment.Exit(1);
            }

            ConsoleWriteLine("Configuration is OK", ConsoleColor.Green);
        }

        private static SettingsJDO ReadSettingsJSON(string path)
        {
            ConsoleWriteLine("Read Configuration File from: " + GetApplicationFolder() + "\\" + path, ConsoleColor.Blue);

            SettingsJDO? _settings = null;

            try
            {
                _settings = JsonConvert.DeserializeObject<SettingsJDO>(File.ReadAllText(path));
            }
            catch { }

            if (_settings == null)
            {
                _settings = new();
                File.WriteAllText(path, JsonConvert.SerializeObject(_settings, Formatting.Indented));

                ConsoleWriteLine("Read Configuration File FAILED. Created default configuration file.", ConsoleColor.Red);
            }

            return _settings;
        }
    }
}