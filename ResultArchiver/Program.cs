﻿using Newtonsoft.Json;
using ResultArchiver.Classes;
using ResultArchiver.Settings;
using System.IO.Compression;

namespace ResultArchiver
{
    internal class Program
    {
        public static SettingsJDO _settings { get; set; } = new SettingsJDO();

        static void Main(string[] args)
        {
            bool CloseApp = false;

            ShowInfo();

            _settings = ReadSettingsJSON(Constants.SETTINGS_PATH);

            CheckSettings(_settings);

            SetupWatcher(_settings.FileFoldeCheckerSettings);

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
            Console.WriteLine("New File Created: " + e.FullPath);

            string destinationPath = Path.ChangeExtension(_settings.DestinationPath + @"\\" + Path.GetFileName(e.FullPath), ".zip");

            Thread.Sleep(1000);

            bool archiveFileError = ArchiveFile(e, destinationPath);

            if (_settings.DeleteResultAfterArchivate && archiveFileError == false)
            {
                DeleteOriginalFileFile(e, destinationPath);
            }
        }

        private static void DeleteOriginalFileFile(FileSystemEventArgs e, string destinationPath)
        {
            try
            {
                Console.WriteLine("Check if Archive exist.");

                if (File.Exists(destinationPath))
                {
                    Console.WriteLine("Archive exist. Deleting original file. Path: " + e.FullPath);
                    File.Delete(e.FullPath);
                }

                Console.WriteLine("Deleting DONE.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error while checking or deleting file: " + ex.Message);
            }
        }

        private static bool ArchiveFile(FileSystemEventArgs e, string destinationPath)
        {
            bool error = false;

            try
            {
                Console.WriteLine("Archivate file: " + e.Name);

                using (ZipArchive archive = ZipFile.Open(destinationPath, ZipArchiveMode.Create))
                {
                    archive.CreateEntryFromFile(e.FullPath, Path.GetFileName(e.FullPath));
                }

                Console.WriteLine("Archiving DONE. Archive Path: " + destinationPath);
            }
            catch (Exception ex)
            {
                error = true;
                Console.WriteLine("Archiving FAILED. Error: " + ex.Message);
            }

            return error;
        }

        private static void ShowInfo()
        {
            Console.WriteLine(Constants.CONSOLE_APP_NAME_TEXT);
            Console.WriteLine(Constants.ABOUT_APP_TABLE);
            Console.WriteLine("");
            Console.WriteLine("Result Archiver Application STARTED.");
        }

        private static void SetupWatcher(FileFoldeCheckerSettingsJDO settings)
        {
            Console.WriteLine("Setuping File/Folder Change Watcher");

            try
            {
                using var watcher = new FileSystemWatcher(settings.Path);

                watcher.NotifyFilter = NotifyFilters.FileName;

                watcher.Created += OnCreated;

                watcher.Filter = "*" + settings.Extension;
                watcher.IncludeSubdirectories = settings.IncludeSubdirectories;
                watcher.EnableRaisingEvents = true;

                Console.WriteLine("Setuping File/Folder Change Watcher DONE");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Setuping File/Folder Change Watcher ERROR: " + ex.Message);
            }
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
            Console.WriteLine("Check Configuration");

            bool error = false;

            if (Directory.Exists(settings!.FileFoldeCheckerSettings.Path) == false)
            {
                error = true;
            }

            if (Directory.Exists(settings!.DestinationPath) == false)
            {
                error = true;
            }

            if (error)
            {
                Console.WriteLine("CONFIG ERROR. CLOSING APPLICATION");
                Thread.Sleep(5000);
                Environment.Exit(1);
            }

            Console.WriteLine("Configuration is OK");
        }

        private static SettingsJDO ReadSettingsJSON(string path)
        {
            Console.WriteLine("Read Configuration File from: " + GetApplicationFolder() + "\\" + path);

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
                Console.WriteLine("Read Configuration File FAILED. Created default configuration file.");
            }

            return _settings;
        }
    }
}