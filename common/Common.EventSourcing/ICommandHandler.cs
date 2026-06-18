namespace QuantInfra.Common.EventSourcing;

public interface ICommandHandler<T> where T : ICommand
{
    void Handle(T cmd);
    // Task HandleAsync(T cmd);
}
