namespace QuantInfra.Common.Infrastructure.Abstractions;

public interface ISecretProvider
{
    byte[] GetOrCreateMasterSecret();
}