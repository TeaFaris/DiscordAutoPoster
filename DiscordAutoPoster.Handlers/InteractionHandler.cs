using Discord.Interactions;
using Discord.WebSocket;
using DiscordAutoPoster.Services;
using DiscordAutoPoster.Services.AuthorizeServices;

namespace Handlers
{
    public class InteractionHandler : IInitializeService
    {
        readonly DiscordSocketClient discordClient;
        readonly InteractionService interactionService;
        readonly IServiceProvider serviceProvider;
        readonly AuthorizeService authorizeService;
        public InteractionHandler(
                DiscordSocketClient discordClient,
                InteractionService interactionService,
                AuthorizeService authorizeService,
                IServiceProvider serviceProvider
            )
        {
            this.authorizeService = authorizeService;
            this.serviceProvider = serviceProvider;
            this.interactionService = interactionService;
            this.discordClient = discordClient;
        }

        public Task InitializeAsync()
        {
            discordClient.InteractionCreated += InteractionCreated;

            return Task.CompletedTask;
        }

        private async Task InteractionCreated(SocketInteraction arg)
        {
            await authorizeService.AddNewUser(arg.User);

            var ctx = new SocketInteractionContext(discordClient, arg);

            await interactionService.ExecuteCommandAsync(ctx, serviceProvider);
        }
    }
}
