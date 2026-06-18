namespace QuantInfra.Services.AccountsCore;

public interface IOutputToInputDisruptorPublisher
{
    void PublishMessage(string senderCompId, object o);
}