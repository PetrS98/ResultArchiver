using System.IO.Compression;

namespace ResultArchiver.Settings
{
    public class SettingsJDO
    {
        public string DestinationPath { get; set; } = "";
        public bool DeleteResultAfterArchivate { get; set; } = false;
        public int MaxAmountOfArchiveFileInFolder { get; set; } = 500;
        public bool EnableLoging { get; set; } = false;
        public CompressionLevel CompressionLevel { get; set; } = 0;
        public FileFoldeCheckerSettingsJDO FileFoldeCheckerSettings { get; set; } = new FileFoldeCheckerSettingsJDO();
    }
}
