using KeeperData.Core.ApiClients.DataBridgeApi;
using KeeperData.Core.Attributes;
using KeeperData.Core.Documents;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Sam.Updates.Steps;

[StepOrder(4)]
public class SamHoldingUpdatePersistenceStep(
    IGenericRepository<SamHoldingDocument> silverHoldingRepository,
    IGenericRepository<SiteDocument> goldSiteRepository,
    ILogger<SamHoldingUpdatePersistenceStep> logger)
    : ImportStepBase<SamHoldingUpdateContext>(logger)
{
    private readonly IGenericRepository<SamHoldingDocument> _silverHoldingRepository = silverHoldingRepository;
    private readonly IGenericRepository<SiteDocument> _goldSiteRepository = goldSiteRepository;

    protected override async Task ExecuteCoreAsync(SamHoldingUpdateContext context, CancellationToken cancellationToken)
    {
        if (context is not { RawHolding.CHANGE_TYPE: DataBridgeConstants.ChangeTypeUpdate })
            return;

        // TODO - Add implementation

        await Task.CompletedTask;
    }
}