using AutoFixture.Kernel;
using KeeperData.Core.ApiClients.DataBridgeApi.Contracts;
using KeeperData.Tests.Common.Generators;

namespace KeeperData.Tests.Common.SpecimenBuilders;

public class SamPortBuilder(
    string fixedChangeType,
    int batchId,
    string cph,
    bool allowNulls = true) : ISpecimenBuilder
{
    private readonly Random _random = new();

    private readonly string _fixedChangeType = fixedChangeType;
    private readonly int _batchId = batchId;
    private readonly string _cph = cph;
    private readonly bool _allowNulls = allowNulls;

    public object Create(object request, ISpecimenContext context)
    {
        if (request is Type type && type == typeof(SamPort))
        {
            return new SamPort
            {
                CPH = _cph,

                PREMISES_NAME = _allowNulls && _random.Next(2) == 0 ? null : $"Port Terminal {Guid.NewGuid().ToString()[..8]}",
                ADDRESS_LINE_1 = _allowNulls && _random.Next(2) == 0 ? null : $"{_random.Next(1, 999)} Harbour Road",
                ADDRESS_LINE_2 = _allowNulls && _random.Next(2) == 0 ? null : $"Dock {_random.Next(1, 20)}",
                ADDRESS_LINE_3 = _allowNulls && _random.Next(2) == 0 ? null : $"{_random.Next(1, 999)} Port City",
                POSTCODE = _allowNulls && _random.Next(2) == 0 ? null : $"L{_random.Next(1, 9)} {_random.Next(1, 9)}{(char)('A' + _random.Next(0, 26))}{(char)('A' + _random.Next(0, 26))}",
                MAP_REFERENCE = AddressGenerator.GenerateMapReference(_allowNulls),
                EASTING = _allowNulls && _random.Next(2) == 0 ? null : _random.Next(100000, 999999),
                NORTHING = _allowNulls && _random.Next(2) == 0 ? null : _random.Next(200000, 999999),

                BATCH_ID = _batchId,
                CHANGE_TYPE = _fixedChangeType
            };
        }

        return new NoSpecimen();
    }
}