using Binance.Futures.USDM;

namespace QuantInfra.Connectors.Binance.Futures.Usdm.Tests;

public class SignatureTests
{
    [Test]
    public void BuildSignaturePayload_JoinsParametersInSortedOrder()
    {
        var parameters = new SortedDictionary<string, object>
        {
            ["timestamp"] = 1499827319559,
            ["quantity"] = 1,
            ["symbol"] = "LTCBTC"
        };

        var payload = parameters.BuildSignaturePayload();

        Assert.That(payload, Is.EqualTo("quantity=1&symbol=LTCBTC&timestamp=1499827319559"));
    }

    [Test]
    public void GetHmacSha256Signature_KnownVector_ReturnsLowercaseHexDigest()
    {
        const string payload = "The quick brown fox jumps over the lazy dog";

        var signature = payload.GetHmacSha256Signature("key");

        Assert.That(signature, Is.EqualTo("f7bc83f430538424b13298e6aa6fb143ef4d59a14946175997479dbc2d1a3cd8"));
    }
}
