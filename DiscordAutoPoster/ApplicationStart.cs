using Discord;
using Discord.WebSocket;
using DiscordAutoPoster.Configuration;
using DiscordAutoPoster.Services.AuthorizeServices;
using DiscordAutoPoster.Services.SlashCommandsServices;
using Handlers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OGA.AppSettings.Writeable.JSONConfig;

namespace DiscordAutoPoster
{
    internal class ApplicationStart : IHostedService
    {
        readonly DiscordSocketClient discordClient;
        readonly IWritableOptions<BotConfiguration> config;
        readonly ILogger<DiscordSocketClient> discordLogger;
        
        readonly AuthorizeService authorizeService;
        readonly SlashCommandHandler slashCommandHandler;
        readonly InteractionHandler interactionHandler;
        readonly SlashCommandsRegisterService slashCommandsRegisterService;

        public ApplicationStart(
                DiscordSocketClient discordClient,
                IWritableOptions<BotConfiguration> config,
                ILogger<DiscordSocketClient> discordLogger,
                AuthorizeService authorizeService,
                SlashCommandHandler slashCommandHandler,
                InteractionHandler interactionHandler,
                SlashCommandsRegisterService slashCommandsRegisterService
            )
        {
            this.slashCommandsRegisterService = slashCommandsRegisterService;
            this.interactionHandler = interactionHandler;
            this.slashCommandHandler = slashCommandHandler;
            this.authorizeService = authorizeService;
            this.discordLogger = discordLogger;
            this.config = config;
            this.discordClient = discordClient;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            discordClient.Log += Log;
            discordClient.Ready += () =>
            {
                discordLogger.LogInformation("Connected as -> [{discordBotName}]", discordClient.CurrentUser.Username);
                return Task.CompletedTask;
            };

            await slashCommandsRegisterService.InitializeAsync();
            await slashCommandHandler.InitializeAsync();
            await interactionHandler.InitializeAsync();
            await authorizeService.InitializeAsync();

            await discordClient.LoginAsync(TokenType.Bot, config.Value.Token);
            await discordClient.StartAsync();
        }

        private Task Log(LogMessage arg)
        {
            var logLevel = arg.Severity switch
            {
                LogSeverity.Info => LogLevel.Information,
                LogSeverity.Critical => LogLevel.Critical,
                LogSeverity.Error => LogLevel.Error,
                LogSeverity.Warning => LogLevel.Warning,
                LogSeverity.Verbose => LogLevel.Trace,
                LogSeverity.Debug => LogLevel.Debug,
                _ => LogLevel.None
            };

            discordLogger.Log(logLevel, "{source}: {message} {exception}", arg.Source, arg.Message, arg.Exception?.ToString() ?? "");
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await discordClient.StopAsync();
            await discordClient.DisposeAsync();
        }
    }
}
