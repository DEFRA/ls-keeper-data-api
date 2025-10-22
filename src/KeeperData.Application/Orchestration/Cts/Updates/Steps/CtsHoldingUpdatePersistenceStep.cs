using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Cts.Updates.Steps;

[StepOrder(4)]
public class CtsHoldingUpdatePersistenceStep(
    IGenericRepository<CtsHoldingDocument> silverHoldingRepository,
    IGenericRepository<SiteDocument> goldSiteRepository,
    ILogger<CtsHoldingUpdatePersistenceStep> logger)
    : ImportStepBase<CtsHoldingUpdateContext>(logger)
{
    private readonly IGenericRepository<CtsHoldingDocument> _silverHoldingRepository = silverHoldingRepository;
    private readonly IGenericRepository<SiteDocument> _goldSiteRepository = goldSiteRepository;

    protected override async Task ExecuteCoreAsync(CtsHoldingUpdateContext context, CancellationToken cancellationToken)
    {
        if (context is not { RawHolding.CHANGE_TYPE: DataBridgeConstants.ChangeTypeUpdate })
            return;

        // TODO - Add implementation

        await Task.CompletedTask;
    }
}