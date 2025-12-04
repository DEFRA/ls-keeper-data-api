using KeeperData.Core.Attributes;
using KeeperData.Core.Documents.Silver;
using KeeperData.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace KeeperData.Application.Orchestration.Updates.Cts.Keepers.Steps;

[StepOrder(3)]
public class CtsUpdateKeeperPersistenceStep(
    IGenericRepository<CtsPartyDocument> silverPartyRepository,
    ILogger<CtsUpdateKeeperPersistenceStep> logger)
    : UpdateStepBase<CtsUpdateKeeperContext>(logger)
{
    private readonly IGenericRepository<CtsPartyDocument> _silverPartyRepository = silverPartyRepository;

    protected override async Task ExecuteCoreAsync(CtsUpdateKeeperContext context, CancellationToken cancellationToken)
    {
        if (context.SilverParty != null)
        {
            await UpsertSilverPartyAsync(context.SilverParty, cancellationToken);
        }
    }

    private async Task UpsertSilverPartyAsync(CtsPartyDocument incomingParty, CancellationToken cancellationToken)
    {
        var existingParty = await _silverPartyRepository.FindOneAsync(
            x => x.PartyId == incomingParty.PartyId &&
                 x.CountyParishHoldingNumber == incomingParty.CountyParishHoldingNumber,
            cancellationToken);

        incomingParty.Id = existingParty?.Id ?? Guid.NewGuid().ToString();

        if (existingParty != null)
        {
            await _silverPartyRepository.UpdateAsync(incomingParty, cancellationToken);
        }
        else
        {
            await _silverPartyRepository.AddAsync(incomingParty, cancellationToken);
        }
    }
}