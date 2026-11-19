using FluentAssertions;
using KeeperData.Application.Queries.Pagination;
using KeeperData.Core.Exceptions;

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

    [Theory]
    [InlineData("not-base-64!!!")]
    [InlineData("U21pdGh8MTIz=")]   // wrong padding
    [InlineData("U21pdGh8MTI")]     // truncated
    public void Decode_WithInvalidBase64_ShouldThrowInvalidCursorException(string cursor)
    {
        var act = () => CursorHelper.Decode(cursor);
        act.Should().Throw<InvalidCursorException>();
    }

    [Theory]
    [InlineData("U21pdGgxMjM=")]    // "Smith123" - no delimiter
    [InlineData("U21pdGh8")]        // "Smith|" - no record id
    [InlineData("U21pdGh8ICA=")]    // "Smith|  " - whitespace record id
    [InlineData("fA==")]            // "|" - delimiter only
    [InlineData("/w==")]            // 0xFF - not valid UTF-8
    public void Decode_WithInvalidDecodedStructure_ShouldThrowInvalidCursorException(string cursor)
    {
        var act = () => CursorHelper.Decode(cursor);
        act.Should().Throw<InvalidCursorException>();
    }

    [Theory]
    [InlineData("U21pdGh8 MTIz")]   // embedded space, e.g. a '+' lost to missing URL encoding
    [InlineData(" U21pdGh8MTIz")]   // leading space
    [InlineData("U21pdGh8MTIz\n")]  // trailing newline
    public void Decode_WithWhitespaceInCursor_ShouldThrowInvalidCursorException(string cursor)
    {
        var act = () => CursorHelper.Decode(cursor);
        act.Should().Throw<InvalidCursorException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void TryDecode_WithAbsentCursor_ShouldReturnFalse(string? cursor)
    {
        CursorHelper.TryDecode(cursor, out _).Should().BeFalse();
        CursorHelper.IsAbsent(cursor).Should().BeTrue();
    }

    [Theory]
    [InlineData("not-base-64!!!")]
    [InlineData("U21pdGgxMjM=")]
    [InlineData("U21pdGh8")]
    public void TryDecode_WithMalformedCursor_ShouldReturnFalse(string cursor)
    {
        CursorHelper.TryDecode(cursor, out _).Should().BeFalse();
        CursorHelper.IsAbsent(cursor).Should().BeFalse();
    }

    [Theory]
    [InlineData("Smith", "123")]
    [InlineData("", "123")]
    [InlineData(null, "123")]
    [InlineData("Smith|Jones", "123")]
    [InlineData("|", "123")]
    [InlineData("Ünïcødé ✓", "ceef6fbc-cc67-4272-9c40-00ae257e62e0")]
    public void Decode_WithEncodedCursor_ShouldRoundTrip(string? sortValue, string id)
    {
        var cursor = CursorHelper.Encode(sortValue, id);

        var result = CursorHelper.Decode(cursor);
        result.Should().NotBeNull();
        result!.Value.sortValue.Should().Be(sortValue ?? "");
        result.Value.id.Should().Be(id);
    }
}