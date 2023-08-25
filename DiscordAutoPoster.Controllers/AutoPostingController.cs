using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordAutoPoster.Configuration;
using DiscordAutoPoster.Models;
using DiscordAutoPoster.Services.Repositories.AutoPostRepositories;
using DiscordAutoPoster.Services.Repositories.UserRepositories;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OGA.AppSettings.Writeable.JSONConfig;

namespace Controllers
{
    public class AutoPostingController : InteractionModuleBase<SocketInteractionContext>
    {
        readonly IUserRepository userRepository;
        readonly IWritableOptions<BotConfiguration> config;
        readonly DiscordSocketClient discordClient;
        readonly IAutoPostRepository autoPostRepository;
        public AutoPostingController(
                IUserRepository userRepository,
                IAutoPostRepository autoPostRepository,
                IWritableOptions<BotConfiguration> config,
                DiscordSocketClient discordClient
            )
        {
            this.autoPostRepository = autoPostRepository;
            this.discordClient = discordClient;
            this.config = config;
            this.userRepository = userRepository;
        }

        public class AutoPostModal : IModal
        {
            public string Title => "Создать объявление";

            [InputLabel("Ваш игровой ник:")]
            [ModalTextInput(nameof(Username), TextInputStyle.Short, minLength: 1, maxLength: 100)]
            [RequiredInput]
            public string Username { get; set; } = null!;

            [InputLabel("Описание:")]
            [ModalTextInput(nameof(Description), TextInputStyle.Paragraph, minLength: 1, maxLength: 1000)]
            [RequiredInput]
            public string Description { get; set; } = null!;
        }

        [SlashCommand("создать-объявление", "Создать объявление.")]
        public async Task CreateAdvertisement()
        {
            var users = await userRepository
                .FindAsync(x => x.DiscordId == Context.User.Id);
            var user = users.First();

            if(user.CurrentAutoPost is not null)
            {
                await RespondAsync("Вы уже создаёте объявление в текущий момент.", ephemeral: true);
                return;
            }

            var guild = discordClient.GetGuild(config.Value.GuildId);
            var guildUser = guild.GetUser(Context.User.Id);

            if (!guildUser.Roles.Any(x => x.Id == config.Value.AllowedRoleId))
            {
                await RespondAsync("У Вас нет доступа к автопостингу!");
                return;
            }

            if (user.MutedUntil > DateTime.UtcNow)
            {
                await RespondAsync($"У Вас стоит ограничение на создание объявлений.\nОграничения снимутся через: {user.MutedUntil - DateTime.UtcNow:hh\\:mm\\:ss}", ephemeral: true);
                return;
            }

            await RespondWithModalAsync<AutoPostModal>(nameof(CreateAutoPostModal));
        }

        [ModalInteraction(nameof(CreateAutoPostModal))]
        public async Task CreateAutoPostModal(AutoPostModal modal)
        {
            var guild = discordClient.GetGuild(config.Value.GuildId);
            var guildUser = guild.GetUser(Context.User.Id);

            var roleToChannels = config.Value.RoleToChannel!;

            var serverKeys = guildUser.Roles.IntersectBy(roleToChannels.Keys.ToArray(), x => x.Id);

            if (!serverKeys.Any())
            {
                await RespondAsync("У Вас нет доступных веток.", ephemeral: true);
                return;
            }

            var menuBuilder = new SelectMenuBuilder()
                .WithPlaceholder("Выберите ветку")
                .WithMinValues(1)
                .WithMaxValues(1);

            foreach (var serverKey in serverKeys)
            {
                var serverChannelsId = roleToChannels[serverKey.Id]
                    .Distinct();

                foreach (var channelID in serverChannelsId)
                {
                    SocketTextChannel ServerChannel = guild.GetTextChannel(channelID);
                    menuBuilder
                        .AddOption(ServerChannel.Name, channelID.ToString());
                }
            }

            menuBuilder.WithCustomId($"ChoseChannelId:{modal.Username},{modal.Description}");

            var builder = new ComponentBuilder()
                .WithSelectMenu(menuBuilder);

            await RespondAsync("Выберите ветку куда постить:", components: builder.Build(), ephemeral: true);
        }

        [ComponentInteraction("ChoseChannelId:*,*")]
        public async Task ChoseChannelId(string username, string description, string[] selectedChannelIds)
        {
            var menuBuilder = new SelectMenuBuilder()
                .WithPlaceholder("Выберите сервер:")
                .WithCustomId($"ChoseServer:{username},{description},{ulong.Parse(selectedChannelIds[0])}")
                .WithMinValues(1)
                .WithMaxValues(1);

            var servers = config.Value.Servers;

            foreach (var server in servers)
            {
                menuBuilder.AddOption(server, server);
            }

            var builder = new ComponentBuilder()
                .WithSelectMenu(menuBuilder);

            await RespondAsync("Выберите игровой сервер:", components: builder.Build(), ephemeral: true);
        }

        [ComponentInteraction("ChoseServer:*,*,*")]
        public async Task ChoosedServer(string username, string description, ulong selectedChannelId, string[] selectedServer)
        {
            var users = await userRepository
                .FindAsync(x => x.DiscordId == Context.User.Id);
            var user = users.First();

            var autoPost = new AutoPost
            {
                Owner = user,
                Username = username,
                Description = description,
                ChannelId = selectedChannelId,
                Server = selectedServer[0],
                LastTimePosted = user.CurrentAutoPost is not null ? user.CurrentAutoPost.LastTimePosted : DateTime.MinValue
            };

            if (user.CurrentAutoPost is not null)
            {
                await autoPostRepository.RemoveAsync(user.CurrentAutoPost);
            }

            await autoPostRepository.AddAsync(autoPost);
            await autoPostRepository.SaveAsync();

            await RespondAsync("Отправьте картинку командой /загрузить-картинку:", ephemeral: true);
        }

        [SlashCommand("загрузить-картинку", "Загружает картинку в объявление.")]
        public async Task UploadImage(IAttachment image)
        {
            if(image.ContentType is not "image/png" or "image/jpeg")
            {
                await RespondAsync("Отправьте картинку формата .png, .jpg или .jpeg.", ephemeral: true);
                return;
            }

            var users = await userRepository
                .FindAsync(x => x.DiscordId == Context.User.Id);
            var user = users.First();

            if (user.CurrentAutoPost is null || user.CurrentAutoPost.Completed)
            {
                await RespondAsync("Вы не создаёте объявление в данный момент.", ephemeral: true);
                return;
            }

            user.CurrentAutoPost.ImagesUrl = new[] { image.Url };

            user.CurrentAutoPost.LastTimePosted = user.CurrentAutoPost.LastTimePosted == DateTime.MinValue
                ? DateTime.UtcNow - TimeSpan.FromMinutes(config.Value.PostDelayInMinutes - 1)
                : user.CurrentAutoPost.LastTimePosted;

            await autoPostRepository.UpdateAsync(user.CurrentAutoPost);
            await autoPostRepository.SaveAsync();

            var whenWouldBePosted = user.CurrentAutoPost.LastTimePosted + TimeSpan.FromMinutes(config.Value.PostDelayInMinutes) - DateTime.UtcNow;

            await RespondAsync("Объявление успешно создано!", ephemeral: true);

            if (whenWouldBePosted.Ticks > 0)
            {
                await RespondAsync($"Ваше объявление будет опубликовано через {whenWouldBePosted:hh\\:mm\\:ss}", ephemeral: true);
            }
        }
    }
}
