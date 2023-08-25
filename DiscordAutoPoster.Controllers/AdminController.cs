using Discord;
using Discord.Interactions;
using DiscordAutoPoster.Configuration;
using DiscordAutoPoster.Services.Repositories.UserRepositories;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OGA.AppSettings.Writeable.JSONConfig;

namespace DiscordAutoPoster.Controllers
{
	public class AdminController : InteractionModuleBase<SocketInteractionContext>
	{
		readonly IUserRepository userRepository;
		readonly IWritableOptions<BotConfiguration> config;

		public AdminController(
				IUserRepository userRepository,
				IWritableOptions<BotConfiguration> config
			)
		{
			this.config = config;
			this.userRepository = userRepository;
		}

		[SlashCommand("роль", "Назначить роль у которой будет доступ к боту.")]
		public async Task SetAllowedRole([Summary("Роль")] IRole role)
		{
			if(!await IsAdmin(Context.User))
				return;

			config.Update(c => c.AllowedRoleId = role.Id);
		}

		[SlashCommand("мут-объявлений", "Запретить отправлять объявления.")]
		public async Task MuteAutopost([Summary("Пользователь")] IUser discordUser, [Summary("Часы")] uint hours)
		{
			if(!await IsAdmin(Context.User))
				return;

			var users = await userRepository.FindAsync(x => x.DiscordId == discordUser.Id);
			var user = users.FirstOrDefault();

			if(user is null)
			{
				await RespondAsync("Данного пользователя нет в базе данных!");
				return;
			}

			user.MutedUntil = DateTime.UtcNow + new TimeSpan((int)hours, 0, 0);

			await userRepository.UpdateAsync(user);
			await userRepository.SaveAsync();

			await RespondAsync($"Вы успешно запретили пользователю {discordUser.Mention} автопостинг на {hours} ч.!");
		}

		[SlashCommand("размут-объявлений", "Разрешить отправлять объявления преждевременно.")]
		public async Task UnmuteAutopost([Summary("Пользователь")] IUser discordUser)
		{
			if(!await IsAdmin(Context.User))
				return;

			var users = await userRepository.FindAsync(x => x.DiscordId == discordUser.Id);
			var user = users.FirstOrDefault();

			if (user is null)
			{
				await RespondAsync("Данного пользователя нет в базе данных!");
				return;
			}

			user.MutedUntil = null;

			await userRepository.SaveAsync();
			await userRepository.UpdateAsync(user);

			await RespondAsync("Успешно!");
		}

		[SlashCommand("добавить-сервер", "Добавляет сервер.")]
		public async Task AddServer([Summary("Сервер")] string server)
		{
			if(!await IsAdmin(Context.User))
				return;

			config.Update(c => c.Servers.Add(server));

			await RespondAsync("Успешно!");
		}

		[SlashCommand("удалить-сервер", "Удаляет сервер.")]
		public async Task RemoveServer([Summary("Сервер")] string server)
		{
			if(!await IsAdmin(Context.User))
				return;

			config.Update(c => c.Servers.Remove(server));

			await RespondAsync("Успешно!");
		}

		[SlashCommand("добавить-привязку", "Добавить привязку роли к текстовому каналу.")]
		public async Task AddBind([Summary("Роль")] IRole role, [Summary("Канал")] ITextChannel textChannel)
		{
			if(!await IsAdmin(Context.User))
				return;

			config.Update(c =>
			{
				if (c.RoleToChannel.TryGetValue(role.Id, out var value))
				{
					value.Add(textChannel.Id);
				}
				else
				{
					c.RoleToChannel.Add(role.Id, new() { textChannel.Id });
				}
			});

			await RespondAsync("Успешно!");
		}

		[SlashCommand("удалить-привязку", "Удалить привязку роли к текстовому каналу.")]
		public async Task RemoveBind([Summary("Роль")] IRole role, [Summary("Канал")] ITextChannel textChannel)
		{
			if(!await IsAdmin(Context.User))
				return;

			if (!config.Value.RoleToChannel.TryGetValue(role.Id, out var value))
			{
				await RespondAsync("Такой привязки не существует!");
				return;
			}

			config.Update(c => value.Remove(textChannel.Id));			

			await RespondAsync("Успешно!");
		}

		[SlashCommand("изменить-задержку", "Изменить задержку между автопостингом.")]
		public async Task ChangeDelay([Summary("Минуты")] uint minutes)
		{
			if(!await IsAdmin(Context.User))
				return;

			config.Update(c => c.PostDelayInMinutes = minutes);

			await RespondAsync($"Успешно, теперь задержка по {minutes} мин.!");
		}

		private async Task<bool> IsAdmin(IUser User)
		{
			if (!config.Value.Admins.Contains(User.Id))
			{
				await RespondAsync("У Вас нет прав на исполненеие данной команды!");
				return false;
			}
			return true;
		}
	}
}
