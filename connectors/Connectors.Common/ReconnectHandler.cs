using Microsoft.Extensions.Logging;

namespace QuantInfra.Connectors.Common
{
    public class ReconnectHandler
    {
        private readonly ILogger _logger;
        private readonly string _clientName;
        private readonly TimeSpan _reconnectInterval;
        private readonly int _maxAttemptsCount;
        private readonly Func<Task> _reconnectFunc;
        
        private int _attemptsLeft;

        public ReconnectHandler(
            ILogger logger,
            string clientName,
            TimeSpan reconnectInterval,
            int maxAttemptsCount,
            Func<Task> reconnectFunc
        )
        {
            _logger = logger;
            _clientName = clientName;
            _reconnectInterval = reconnectInterval;
            _maxAttemptsCount = maxAttemptsCount;
            _attemptsLeft = maxAttemptsCount;
            _reconnectFunc = reconnectFunc;
        }        

        public async Task OnDisconnectAsync(DisconnectionType type)
        {
            var msg = $"{_clientName} disconnected with type {type}";
            switch (type)
            {
                case DisconnectionType.Exit:
                case DisconnectionType.ByUser:
                    _logger.LogInformation(msg);
                    break;
                default:
                    _logger.LogWarning(msg);
                    break;
            }

            do
            {
                try
                {
                    _logger.LogInformation($"Reconnecting, {_attemptsLeft} attempts left");
                    await _reconnectFunc().ConfigureAwait(false);
                    _logger.LogInformation($"{_clientName} reconnected");
                    _attemptsLeft = _maxAttemptsCount;
                    return;
                }
                catch (Exception ex)
                {
                    _attemptsLeft--;
                    var log = $"Reconnect attempt failed, {_attemptsLeft.ToString()} left";
                    if (_attemptsLeft > 0) log += $", trying again after {_reconnectInterval.ToString()}";
                    
                    _logger.LogError(ex, log);

                    if (_attemptsLeft <= 0)
                        Environment.FailFast($"{_clientName} failed to reconnect after {_maxAttemptsCount} attempts");
                }
                
                await Task.Delay(_reconnectInterval).ConfigureAwait(false);
            } while (true);
        }
    }
}