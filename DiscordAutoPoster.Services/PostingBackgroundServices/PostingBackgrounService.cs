using Discord;
using Discord.WebSocket;
using DiscordAutoPoster.Configuration;
using DiscordAutoPoster.Models;
using DiscordAutoPoster.Services.Repositories.AutoPostRepositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OGA.AppSettings.Writeable.JSONConfig;

namespace DiscordAutoPoster.Services.PostingBackgroundServices
{
	public class PostingBackgroundService : BackgroundService
	{
		static TimeSpan autoPostDelay;

		readonly IServiceProvider serviceProvider;
		readonly DiscordSocketClient discordClient;
		readonly IWritableOptions<BotConfiguration> config;
		readonly ILogger<PostingBackgroundService> logger;

		public PostingBackgroundService(
				DiscordSocketClient discordClient,
				IServiceProvider serviceProvider,
				IWritableOptions<BotConfiguration> config,
				ILogger<PostingBackgroundService> logger
			)
		{
			this.logger = logger;
			this.config = config;
			this.discordClient = discordClient;
			this.serviceProvider = serviceProvider;

			autoPostDelay = TimeSpan.FromMinutes(this.config.Value.PostDelayInMinutes);
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				if(discordClient.LoginState != LoginState.LoggedIn || discordClient.ConnectionState != ConnectionState.Connected)
				{
					continue;
				}

				using var scope = serviceProvider.CreateAsyncScope();

				var autoPostRepository = scope.ServiceProvider.GetRequiredService<IAutoPostRepository>();

				var autoPosts = await autoPostRepository.GetAllAsync();

				var autoPostsToRemove = new List<AutoPost>();
				var autoPostsToUpdate = new List<AutoPost>();

				foreach (var autoPost in autoPosts)
				{
					try
					{
						if (autoPost.Owner.MutedUntil > DateTime.UtcNow)
						{
							autoPostsToRemove.Add(autoPost);
							continue;
						}

						if (!(autoPost.ImagesUrl is not null) || autoPost.LastTimePosted + autoPostDelay >= DateTime.UtcNow)
						{
							continue;
						}

						ulong guildId = config.Value.GuildId;
						var guild = discordClient.GetGuild(guildId);
						var user = guild.GetUser(autoPost.Owner.DiscordId);
						var textChannel = guild.GetTextChannel(autoPost.ChannelId);

						if (user is null || !user.Roles.Any(x => x.Id == config.Value.AllowedRoleId))
						{
							autoPostsToRemove.Add(autoPost);
							continue;
						}

						var discriminator = user.Discriminator != "0000"
							? string.Concat('#', user.Discriminator)
							: string.Empty;


						var embedBuilder = new EmbedBuilder()
							.WithDescription($"""
											 `Торговец [тэгом]`
											 <@{user.Id}>
											 `Торговец [текстом]`
											 {user.Username}{discriminator}
											 `Ник`: {autoPost.Username}
											 `Сервер`: {autoPost.Server}
											 
											 Объявление торговца:
											 
											 {autoPost.Description}
											 
											 Активировать услугу <#1096558679215575180>
											 """)
							.WithThumbnailUrl(user.GetAvatarUrl(ImageFormat.Png))
							.WithImageUrl(autoPost.ImagesUrl![0])
							.WithColor(Color.Blue);

						await textChannel.SendMessageAsync(embed: embedBuilder.Build());

						autoPost.LastTimePosted = DateTime.UtcNow;
						autoPostsToUpdate.Add(autoPost);
					}
					catch (Exception ex)
					{
						logger.LogError(ex, "Error occurred while posting.");
					}
				}

				await autoPostRepository.RemoveRangeAsync(autoPostsToRemove);
				await autoPostRepository.UpdateRangeAsync(autoPostsToUpdate);
				await autoPostRepository.SaveAsync();

				await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
			}
		}
	}
}
