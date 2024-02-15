using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GhostLyzer.Core.EventStoreDB.BackgroundWorkers
{
    /// <summary>
    /// Represents a background worker that runs a specified task.
    /// </summary>
    public class BackgroundWorker : BackgroundService
    {
        private readonly ILogger<BackgroundWorker> _logger;
        private readonly Func<CancellationToken, Task> _task;

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgroundWorker"/> class.
        /// </summary>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="task">The task to run in the background.</param>
        public BackgroundWorker(ILogger<BackgroundWorker> logger, Func<CancellationToken, Task> task)
        {
            _logger = logger;
            _task = task;
        }

        /// <summary>
        /// Executes the background task.
        /// </summary>
        /// <param name="stoppingToken">A cancellation token that can be used to stop the task.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.Run(async () =>
        {
            await Task.Yield();
            _logger.LogInformation("Background Worker Stopped");

            await _task(stoppingToken);
            _logger.LogInformation("Background Worker Stopped");
        }, stoppingToken);
    }
}
