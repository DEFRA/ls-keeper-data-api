using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Core.Domain.Enums;

namespace KeeperData.Core.Domain.Parties.Rules;

public static class PartyTypeRules
{
    /// <summary>
    /// TBC
    /// </summary>
    /// <param name="party"></param>
    /// <returns></returns>
    public static PartyType DeterminePartyType(this CtsAgentOrKeeper party)
    {
        if (!string.IsNullOrWhiteSpace(party.PAR_SURNAME)
            && !string.IsNullOrWhiteSpace(party.PAR_INITIALS))
            return PartyType.Person;

        return PartyType.Business;
    }

    /// <summary>
    /// TBC
    /// </summary>
    /// <param name="party"></param>
    /// <returns></returns>
    public static PartyType DeterminePartyType(this SamCphHolder holder)
    {
        if (!string.IsNullOrWhiteSpace(holder.ORGANISATION_NAME))
            return PartyType.Business;

        return PartyType.Person;
    }

    /// <summary>
    /// TBC
    /// </summary>
    /// <param name="party"></param>
    /// <returns></returns>
    public static PartyType DeterminePartyType(this SamParty party)
    {
        if (!string.IsNullOrWhiteSpace(party.ORGANISATION_NAME))
            return PartyType.Business;

        return PartyType.Person;
    }
}