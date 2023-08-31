using Data;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using DiscordAutoPoster;
using DiscordAutoPoster.Configuration;
using DiscordAutoPoster.Services.AuthorizeServices;
using DiscordAutoPoster.Services.PostDateRegulatorServices;
using DiscordAutoPoster.Services.PostingBackgroundServices;
using DiscordAutoPoster.Services.Repositories.AutoPostRepositories;
using DiscordAutoPoster.Services.Repositories.UserRepositories;
using DiscordAutoPoster.Services.SlashCommandsServices;
using Handlers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OGA.AppSettings.Writeable.JSONConfig;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
	.AddWriteableJsonFile("appsettings.json");

#if DEBUG
builder.Configuration
	.AddUserSecrets(typeof(Program).Assembly);
#endif

builder.Services
	.ConfigureWritable<BotConfiguration>(builder.Configuration.GetSection("Bot"));

var discordConfig = new DiscordSocketConfig
{
	GatewayIntents = GatewayIntents.All,
	HandlerTimeout = Timeout.Infinite,
	AlwaysDownloadUsers = true
};
var interactionServiceConfig = new InteractionServiceConfig { DefaultRunMode = Discord.Interactions.RunMode.Async };

builder.Services.AddSingleton(provider => provider);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
	?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseNpgsql(connectionString));

using (var provider = builder.Services.BuildServiceProvider())
{
	var context = provider.GetRequiredService<ApplicationDbContext>();
	if (context.Database.GetPendingMigrations().Any())
	{
		context.Database.Migrate();
	}
}

// Hosted Services
builder.Services.AddHostedService<PostDateRegulatorService>();
builder.Services.AddHostedService<ApplicationStart>();
builder.Services.AddHostedService<PostingBackgroundService>();

// Configurations
builder.Services.AddSingleton(discordConfig);
builder.Services.AddSingleton(interactionServiceConfig);

// Services
builder.Services.AddSingleton<DiscordSocketClient>();
builder.Services.AddSingleton<CommandService>();
builder.Services.AddSingleton<InteractionService>();
builder.Services.AddSingleton<AuthorizeService>();
builder.Services.AddSingleton<SlashCommandsRegisterService>();

//// Handlers
builder.Services.AddSingleton<InteractionHandler>();
builder.Services.AddSingleton<SlashCommandHandler>();

//// Repositories
builder.Services.AddTransient<IUserRepository, UserRepository>();
builder.Services.AddTransient<IAutoPostRepository, AutoPostRepository>();

var app = builder.Build();

await app.StartAsync();
await app.WaitForShutdownAsync();