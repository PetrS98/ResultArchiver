using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResultArchiver.Classes
{
    public static class Constants
    {
        public readonly static string CONSOLE_APP_NAME_TEXT = "  _____                 _ _                       _     _                \r\n |  __ \\               | | |       /\\            | |   (_)               \r\n | |__) |___  ___ _   _| | |_     /  \\   _ __ ___| |__  ___   _____ _ __ \r\n |  _  // _ \\/ __| | | | | __|   / /\\ \\ | '__/ __| '_ \\| \\ \\ / / _ \\ '__|\r\n | | \\ \\  __/\\__ \\ |_| | | |_   / ____ \\| | | (__| | | | |\\ V /  __/ |   \r\n |_|  \\_\\___||___/\\__,_|_|\\__| /_/    \\_\\_|  \\___|_| |_|_| \\_/ \\___|_|   \r\n                                                                         \r\n                                                                         ";
        
        public readonly static string ABOUT_APP_TABLE = "+-----------------------------------+------------------------------------+\r\n" 
                                                      + "|        APPLICATION VERSION:       |               V 1.0.0              |\r\n"
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
    }
}
