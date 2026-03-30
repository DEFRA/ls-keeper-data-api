using FluentAssertions;
using KeeperData.Application.Queries.Pagination;

namespace KeeperData.Application.Tests.Unit.Queries.Pagination;

public class CursorHelperTests
{
    [Fact]
    public void Encode_WithValidInputs_ShouldReturnBase64String()
    {
        var result = CursorHelper.Encode("Smith", "123");
        result.Should().Be("U21pdGh8MTIz"); // "Smith|123" in base64
    }

    [Fact]
    public void Encode_WithNullSortValue_ShouldHandleGracefully()
    {
        var result = CursorHelper.Encode(null, "123");
        result.Should().Be("fDEyMw==");
    }

    [Fact]
    public void Decode_WithValidEncodedString_ShouldReturnTuple()
    {
        var result = CursorHelper.Decode("U21pdGh8MTIz");
        result.Should().NotBeNull();
        result!.Value.sortValue.Should().Be("Smith");
        result.Value.id.Should().Be("123");
    }

    [Fact]
    public void Decode_WithNullSortValueEncodedString_ShouldReturnTuple()
    {
        var result = CursorHelper.Decode("fDEyMw==");
        result.Should().NotBeNull();
        result!.Value.sortValue.Should().Be("");
        result.Value.id.Should().Be("123");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Decode_WithNullOrWhitespace_ShouldReturnNull(string? cursor)
    {
        var result = CursorHelper.Decode(cursor);
        result.Should().BeNull();
    }

    [Fact]
    public void Decode_WithInvalidBase64_ShouldReturnNull()
    {
        var result = CursorHelper.Decode("not-base-64!!!");
        result.Should().BeNull();
    }

    [Fact]
    public void Decode_WithValidBase64ButNoDelimiter_ShouldReturnNull()
    {
        var result = CursorHelper.Decode("U21pdGgxMjM=");
        result.Should().BeNull();
    }
}