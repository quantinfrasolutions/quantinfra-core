namespace QuantInfra.Common.ServiceBase;

public interface IComponentExceptionHandler
{
    void Raise(Exception exception);
}