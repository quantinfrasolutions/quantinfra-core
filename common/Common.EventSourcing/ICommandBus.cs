namespace QuantInfra.Common.EventSourcing;

public interface ICommandBus
{
    void SendCommand<T>(T command) where T : ICommand;
    void SendAnonymousCommand(object command);
    // Task SendCommandAsync<T>(T command) where T : ICommand;
}