using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Eva.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tweetinvi;

namespace Eva.Modules
{
    // Create a module with no prefix
    /// <summary>
    /// Common commands module
    /// </summary>
    [Name("Common")]
    [Summary("Common commands for DiVA")]
    public class Common : ModuleBase
    {
        private readonly CommandService _service;
        private readonly IConfigurationRoot _config;
        private readonly DiscordSocketClient _client;

        /// <summary>
        /// Override ReplyAsync
        /// </summary>
        /// <param name="message"></param>
        /// <param name="isTTS"></param>
        /// <param name="embed"></param>
        /// <param name="options"></param>
        /// <param name="deleteafter"></param>
        /// <returns></returns>
        protected async Task<IUserMessage> ReplyAsync(string message = null, bool isTTS = false, Embed embed = null, RequestOptions options = null, TimeSpan? deleteafter = null)
        {
            var msg = await base.ReplyAsync(message, isTTS, embed, options);
            if (deleteafter != null)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(deleteafter.Value.TotalMilliseconds));
                await msg.DeleteAsync();
            }
            return msg;
        }

        /// <summary>
        /// Common Commands module builder
        /// </summary>
        /// <param name="service"></param>
        public Common(CommandService service)
        {
            _client = Eva.client;
            _service = service;
            _config = Eva.Configuration;
        }
        #region COMMANDS

        #region Help
        /// <summary>
        /// HELP - Displays some help
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        [Command("help")]
        [Alias("h")]
        [Summary("Prints the help of available commands")]
        public async Task HelpAsync(string command = null)
        {
            if (command == null)    //______________________________________________        HELP WITH NO COMMAND PROVIDED
            {
                try
                { await Context.Message.DeleteAsync(); }
                catch { }
                string prefix = _config["prefix"];
                var builder = new EmbedBuilder()
                {
                    Color = new Color(114, 137, 218),
                    Title = "Help",
                    Description = "These are the commands you can use"
                };

                foreach (var module in _service.Modules)
                {
                    string description = "```";
                    foreach (var cmd in module.Commands)
                    {
                        var result = await cmd.CheckPreconditionsAsync(Context);
                        if (result.IsSuccess)
                        {
                            string alias = cmd.Aliases.First();
                            description += $"{prefix}{alias.PadRight(10)}\t{cmd.Summary}\n";
                        }
                    }
                    description += "```";


                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        builder.AddField(x =>
                        {
                            x.Name = module.Name;
                            x.Value = description;
                            x.IsInline = false;
                        });
                    }
                }
                await ReplyAsync("", false, builder.Build());
            }
            else //_________________________________________________________________       HELP WITH COMMAND PROVIDED
            {
                var result = _service.Search(Context, command);
                if (!result.IsSuccess)
                {
                    await ReplyAsync($"Sorry, I couldn't find a command like **{command}**.");
                    return;
                }
                var builder = new EmbedBuilder()
                {
                    Color = new Color(114, 137, 218),
                    Description = $"Here are some commands like **{command}**"
                };

                foreach (var match in result.Commands)
                {
                    var cmd = match.Command;
                    builder.AddField(x =>
                    {
                        x.Name = $"({string.Join("|", cmd.Aliases)})";
                        x.Value = $"Parameters: {string.Join(", ", cmd.Parameters.Select(p => p.Name))}\n" +
                                  $"Summary: {cmd.Summary}";
                        x.IsInline = false;
                    });
                }
                await ReplyAsync("", false, builder.Build());
            }
        }
        #endregion Help

        #region version
        /// <summary>
        /// VERSION - Command Version
        /// </summary>
        /// <returns></returns>
        [Command("version"), Summary("Check the bot's version")]
        [Alias("v")]
        public async Task Version()
        {
            try
            { await Context.Message.DeleteAsync(); }
            catch { }
            var arch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;
            var OSdesc = System.Runtime.InteropServices.RuntimeInformation.OSDescription;

            var builder = new EmbedBuilder();

            builder.WithTitle($"Bot Informations");
            builder.WithDescription($"{_client.CurrentUser.Mention} - {_client.CurrentUser.Username}#{_client.CurrentUser.Discriminator}");

            EmbedFieldBuilder field = new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Bot : ",
                Value = Assembly.GetExecutingAssembly().GetName().Name
            };
            builder.AddField(field);

            field = new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Version : ",
                Value = 'v' + Eva.GetVersion()
            };
            builder.AddField(field);

            field = new EmbedFieldBuilder
            {
                IsInline = true,
                Name = "Running on : ",
                Value = $"{OSdesc} ({arch})"
            };
            builder.AddField(field);

            builder.WithThumbnailUrl(_client.CurrentUser.GetAvatarUrl());
            builder.WithColor(Color.Blue);

            var embed = builder.Build();
            await Context.Channel.SendMessageAsync("", false, embed);
        }
        #endregion version

        #region quote
        /// <summary>
        /// STATUS - Command status
        /// </summary>
        /// <param name="stat"></param>
        /// <returns></returns>
        [Command("quote")]
        [Summary("Quote a tweet")]
        [RequireOwner]
        [RequireContext(ContextType.DM)]
        public async Task Quote(string url = "")
        {
            try
            {
                var id = Int64.Parse(url.Split('/')[url.Split('/').Length-1]);
                var tweet = Tweet.GetTweet(id);
                if (TweetService.CheckTweet(tweet, User.GetAuthenticatedUser()))
                {
                    Log.Message(Log.neutral, $"QUOTING {tweet.CreatedBy.ScreenName}\n{tweet.Text}\n{tweet.Media.Count} media files", "Discord Quote");
                    var response = TweetService.SendTweet(tweet);
                    await ReplyAsync($"Tweet have been quoted : {response.Url}");
                }
            }
            catch
            { Log.Message(Log.warning, "Whoopsie", "Discord Quote"); }
            
        }
        #endregion quote

        #region loglvl
        [Command("loglvl"), Summary("Sets logs severity")]
        [RequireOwner]
        [RequireContext(ContextType.DM)]
        public async Task LogLevel(ushort log = 10)
        {
            try
            { await Context.Message.DeleteAsync(); }
            catch { }
            if (log <= 5)
            {
                Eva.logLvl = log;
                await Context.User.SendMessageAsync($"Setting Log Level to {log}");
            }
            else
            { await Context.User.SendMessageAsync($"Please enter a value between 0 (Critical Messages) and 5(Debug messages)"); }
        }
        #endregion

        #endregion COMMANDS
    }
}
