﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResultArchiver.Classes
{
    public static class Constants
    {
        public readonly static string CONSOLE_APP_NAME_TEXT = "                                                                         \r\n  _____                 _ _                       _     _                \r\n |  __ \\               | | |       /\\            | |   (_)               \r\n | |__) |___  ___ _   _| | |_     /  \\   _ __ ___| |__  ___   _____ _ __ \r\n |  _  // _ \\/ __| | | | | __|   / /\\ \\ | '__/ __| '_ \\| \\ \\ / / _ \\ '__|\r\n | | \\ \\  __/\\__ \\ |_| | | |_   / ____ \\| | | (__| | | | |\\ V /  __/ |   \r\n |_|  \\_\\___||___/\\__,_|_|\\__| /_/    \\_\\_|  \\___|_| |_|_| \\_/ \\___|_|   \r\n                                                                         \r\n                                                                         ";
        
        public readonly static string ABOUT_APP_TABLE = "                                                                          \r\n"
                                                      + "+-----------------------------------+------------------------------------+\r\n" 
                                                      + "|        APPLICATION VERSION:       |               V 1.0.4              |\r\n"
                                                      + "+-----------------------------------+------------------------------------+\r\n"
                                                      + "|               This application only for Nexen Tire Czech.              |\r\n"
                                                      + "+-----------------------------------+------------------------------------+\r\n"
                                                      + "|             DEVELOPER:            |             Petr Staněk            |\r\n"
                                                      + "+-----------------------------------+------------------------------------+\r\n"
                                                      + "|               EMAIL:              |      petr.stanek@nexentire.com     |\r\n"
                                                      + "+-----------------------------------+------------------------------------+\r\n"
                                                      + "|           PHONE NUMBER:           |          +420 703 496 310          |\r\n"
                                                      + "+-----------------------------------+------------------------------------+\r\n";

        public static readonly string SETTINGS_PATH = "settings.json";

        public static readonly string APPLICATION_NAME = "Nexen Tire - Result Archiver V 1.0.0";

        public static readonly string LOG_PATH = "logs/Log.txt";

        public static readonly string[] SIZE_SUFFIXIES = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

        public static readonly long MIN_FREE_SPACE_ON_DRIVE = 5368709120; // In bytes, In GB: 5.3687 GB
        public static readonly long MAX_SIZE_OF_ARCHIVE_FILE = 104857600; // In bytes, In MB: 104.86 MB
    }
}
