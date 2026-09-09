using APP.Services.Formulas;
using Xunit;

namespace APP.Tests.Repository;

public class FormulaCanonicalJsonTests
{
    [Fact]
    public void Canonicalize_SortsKeysAndPreservesArrayOrder()
    {
        var actual = FormulaCanonicalJson.Canonicalize(
            "{\"z\":[2,1],\"a\":{\"d\":4,\"c\":3}}", 1024);

        Assert.Equal("{\"a\":{\"c\":3,\"d\":4},\"z\":[2,1]}", actual);
    }

    [Fact]
    public void Canonicalize_RejectsDuplicateKeysAndNonIntegerNumbers()
    {
        Assert.Throws<ArgumentException>(() =>
            FormulaCanonicalJson.Canonicalize("{\"a\":1,\"a\":2}", 1024));
        Assert.Throws<ArgumentException>(() =>
            FormulaCanonicalJson.Canonicalize("{\"a\":1.5}", 1024));
    }

    [Fact]
    public void HashJson_MatchesCrossRuntimeUtf8Fixture()
    {
        var actual = FormulaCanonicalJson.HashJson(
            "oryx:test:v1", "{\"b\":2,\"a\":\"é\"}", 1024);

        Assert.Equal(
            "50f5137f667f301e7022f110b5498fcc3de53c4623a22162186abacaa222306b",
            actual);
    }
}
