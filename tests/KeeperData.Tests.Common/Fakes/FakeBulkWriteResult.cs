using MongoDB.Driver;

namespace KeeperData.Tests.Common.Fakes;

public class FakeBulkWriteResult<T> : BulkWriteResult<T>
{
    public FakeBulkWriteResult()
        : base(requestCount: 0, processedRequests: [])
    {
    }

    public override long DeletedCount => 0;
    public override long InsertedCount => 0;
    public override bool IsAcknowledged => true;
    public override bool IsModifiedCountAvailable => true;
    public override long MatchedCount => 0;
    public override long ModifiedCount => 0;
    public override IReadOnlyList<BulkWriteUpsert> Upserts => [];
}