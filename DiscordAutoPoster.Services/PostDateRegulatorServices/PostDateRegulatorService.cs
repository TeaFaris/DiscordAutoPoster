using DiscordAutoPoster.Services.Repositories.AutoPostRepositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DiscordAutoPoster.Services.PostDateRegulatorServices
{
    public class PostDateRegulatorService : IHostedService
    {
        readonly IServiceProvider serviceProvider;

        public PostDateRegulatorService(IServiceProvider serviceProvider) { this.serviceProvider = serviceProvider; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using var scope = serviceProvider.CreateAsyncScope();

            var autoPostService = scope.ServiceProvider.GetRequiredService<IAutoPostRepository>();

            var autoPosts = (await autoPostService.FindAsync(x => x.Completed)).ToList();
            var now = DateTime.UtcNow;

            for (int i = 0; i < autoPosts.Count; i++)
            {
                var autoPost = autoPosts[i];

                autoPost.LastTimePosted = new DateTime(
                    now.Year,
                    now.Month,
                    now.Day,
                    now.Hour,
                    autoPost.LastTimePosted.Minute + (i * 2),
                    autoPost.LastTimePosted.Second,
                    DateTimeKind.Utc);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
