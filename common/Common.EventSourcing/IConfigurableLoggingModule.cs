namespace QuantInfra.Common.EventSourcing;

public interface IConfigurableLoggingModule
{
    public void EnableLogging();
    public void DisableLogging();
}