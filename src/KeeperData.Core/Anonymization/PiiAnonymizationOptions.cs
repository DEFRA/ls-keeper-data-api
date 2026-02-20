using System.Runtime.InteropServices.ComTypes;

namespace KeeperData.Application.Configuration;

public class PiiAnonymizationOptions
{
    public const string SectionName = "PiiAnonymization";
    public bool Enabled { get; set; }
}