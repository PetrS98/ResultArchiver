using Newtonsoft.Json;
using ResultArchiver.Settings;
using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ResultArchiver.Classes
{
    public class MainService
    {
        private readonly FileSystemWatcher _watcher = new FileSystemWatcher();
        public SettingsJDO _settings { get; set; } = new SettingsJDO();

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

        public void Start()
        {
            bool CloseApp = false;

            DisableCloseAppFunction();

            ShowInfo();

            _settings = ReadSettingsJSON(Constants.SETTINGS_PATH);

            CheckSettings(_settings);

            SetupWatcher(_settings.FileFoldeCheckerSettings, _watcher);

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

        public void Stop()
        {

        }

        private void OnCreated(object sender, FileSystemEventArgs e)
        {
            ConsoleWriteLine("New File Created: " + e.FullPath, ConsoleColor.Blue);

            string destinationPath = Path.ChangeExtension(_settings.DestinationPath + @"\\" + Path.GetFileName(e.FullPath), ".zip");

            Thread.Sleep(1000);

            bool archiveFileError = ArchiveFile(e, destinationPath);

            if (_settings.DeleteResultAfterArchivate && archiveFileError == false)
            {
                DeleteOriginalFileFile(e, destinationPath);
            }
        }

        private void ConsoleWriteLine(string text, ConsoleColor textColor)
        {
            Console.ForegroundColor = textColor;
            Console.WriteLine(text);
            Console.ForegroundColor = ConsoleColor.White;
        }

        private void DisableCloseAppFunction()
        {
            DeleteMenu(GetSystemMenu(GetConsoleWindow(), false), SC_CLOSE, MF_BYCOMMAND);

            // Disable CTRL + C
            Console.CancelKeyPress += delegate (object? sender, ConsoleCancelEventArgs e) {
                e.Cancel = true;
            };
        }

        private void DeleteOriginalFileFile(FileSystemEventArgs e, string destinationPath)
        {
            try
            {
                ConsoleWriteLine("Check if Archive exist.", ConsoleColor.Blue);

                if (File.Exists(destinationPath))
                {
                    ConsoleWriteLine("Archive exist. Deleting original file. Path: " + e.FullPath, ConsoleColor.Blue);
                    File.Delete(e.FullPath);
                }

                ConsoleWriteLine("Deleting DONE.", ConsoleColor.Green);

            }
            catch (Exception ex)
            {
                ConsoleWriteLine("Error while checking or deleting file: " + ex.Message, ConsoleColor.Red);
            }
        }

        private bool ArchiveFile(FileSystemEventArgs e, string destinationPath)
        {
            bool error = false;

            try
            {
                ConsoleWriteLine("Archivate file: " + e.Name, ConsoleColor.Blue);

                using (ZipArchive archive = ZipFile.Open(destinationPath, ZipArchiveMode.Create))
                {
                    archive.CreateEntryFromFile(e.FullPath, Path.GetFileName(e.FullPath));
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

        private void ShowInfo()
        {
            ConsoleWriteLine(Constants.CONSOLE_APP_NAME_TEXT, ConsoleColor.DarkMagenta);
            ConsoleWriteLine(Constants.ABOUT_APP_TABLE, ConsoleColor.DarkMagenta);
            ConsoleWriteLine("", default);
            ConsoleWriteLine("Result Archiver Application STARTED.", ConsoleColor.Blue);
        }

        private void SetupWatcher(FileFoldeCheckerSettingsJDO settings, FileSystemWatcher watcher)
        {
            ConsoleWriteLine("Setuping File/Folder Change Watcher", ConsoleColor.Blue);

            try
            {
                watcher = new FileSystemWatcher(settings.Path);

                watcher.NotifyFilter = NotifyFilters.FileName;

                watcher.Created += OnCreated;

                watcher.Filter = "*" + settings.Extension;
                watcher.IncludeSubdirectories = settings.IncludeSubdirectories;
                watcher.EnableRaisingEvents = true;

                ConsoleWriteLine("Setuping File/Folder Change Watcher DONE", ConsoleColor.Green);
            }
            catch (Exception ex)
            {
                ConsoleWriteLine("Setuping File/Folder Change Watcher ERROR: " + ex.Message, ConsoleColor.Red);
            }
        }

        private string GetApplicationFolder()
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

        private void CheckSettings(SettingsJDO settings)
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

        private SettingsJDO ReadSettingsJSON(string path)
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
