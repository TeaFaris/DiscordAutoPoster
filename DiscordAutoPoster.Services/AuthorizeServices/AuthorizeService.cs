using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordAutoPoster.Services.Repositories.UserRepositories;
using Microsoft.Extensions.DependencyInjection;
using Models;

namespace DiscordAutoPoster.Services.AuthorizeServices
{
    public class AuthorizeService : IInitializeService
    {
        readonly IServiceProvider serviceProvider;
        readonly InteractionService interactionService;
        readonly DiscordSocketClient discordSocketClient;
        public AuthorizeService(
                DiscordSocketClient discordSocketClient,
                InteractionService interactionService,
                IServiceProvider serviceProvider
            )
        {
            this.discordSocketClient = discordSocketClient;
            this.interactionService = interactionService;
            this.serviceProvider = serviceProvider;
        }

        public Task InitializeAsync()
        {
            discordSocketClient.MessageReceived += MessageReceived;
            discordSocketClient.IntegrationCreated += IntegrationCreated;
            interactionService.InteractionExecuted += InteractionExecuted;
            
            return Task.CompletedTask;
        }

        private async Task InteractionExecuted(ICommandInfo arg1, IInteractionContext arg2, IResult arg3)
        {
            await AddNewUser(arg2.User);
        }

        private async Task IntegrationCreated(IIntegration arg)
        {
            await AddNewUser(arg.User);
        }

        private async Task MessageReceived(SocketMessage arg)
        {
            await AddNewUser(arg.Author);
        }

        public async Task AddNewUser(IUser user)
        {
            using var scope = serviceProvider.CreateAsyncScope();

            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            var users = await userRepository.FindAsync(x => x.DiscordId == user.Id);

            if (users.Any())
            {
                return;
            }

            var newUser = new ApplicationUser
            {
                DiscordId = user.Id
            };

            await userRepository.AddAsync(newUser);
            await userRepository.SaveAsync();
        }
    }
}
