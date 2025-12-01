using MongoDB.Driver;

namespace KeeperData.Core.Repositories
{
    public static class IndexDefaults
    {
        public static readonly Collation CollationCaseInsensitive = new Collation(locale: "en", strength: CollationStrength.Primary, caseLevel: false);
    }
}