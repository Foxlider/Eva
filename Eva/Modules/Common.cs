using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Eva.Services;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        /// <param name="isTts"></param>
        /// <param name="embed"></param>
        /// <param name="options"></param>
        /// <param name="deleteafter"></param>
        /// <returns></returns>
        private async Task<IUserMessage> ReplyAsync(string message = null, bool isTts = false, Embed embed = null, RequestOptions options = null, TimeSpan? deleteafter = null)
        {
            var msg = await base.ReplyAsync(message, isTts, embed, options);
            if (deleteafter == null) return msg;
            var t = new Thread(async () =>
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(deleteafter.Value.TotalMilliseconds));
                    await msg.DeleteAsync();
                })
                { IsBackground = true };
            t.Start();
            return msg;
        }

        /// <summary>
        /// Common Commands module builder
        /// </summary>
        /// <param name="service"></param>
        public Common(CommandService service)
        {
            _client = Eva.Client;
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
                catch { /*ignored*/ }
                var prefix = _config["prefix"];
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
            catch { /*ignored*/}
            var arch = System.Runtime.InteropServices.RuntimeInformation.OSArchitecture;
            var osDesc = System.Runtime.InteropServices.RuntimeInformation.OSDescription;

            var builder = new EmbedBuilder();

            builder.WithTitle("Bot Informations");
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
                Value = $"{osDesc} ({arch})"
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
        /// <returns></returns>
        [Command("quote", RunMode = RunMode.Async)]
        [Summary("Quote a tweet")]
        [RequireOwner]
        [RequireContext(ContextType.DM)]
        public async Task Quote(string url = "")
        {
            try
            {
                var id = Int64.Parse(url.Split('/')[url.Split('/').Length - 1]);
                var tweet = Tweet.GetTweet(id);
                if (TweetService.CheckTweet(tweet, User.GetAuthenticatedUser()))
                {
                    Logger.Log(Logger.Neutral, $"QUOTING {tweet.CreatedBy.ScreenName}\n{tweet.Text}\n{tweet.Media.Count} media files", "Discord Quote");
                    var response = TweetService.SendTweet(tweet);
                    await ReplyAsync($"Tweet have been quoted : {response.Url}");
                }
            }
            catch
            { Logger.Log(Logger.Warning, "Whoopsie", "Discord Quote"); }
        }
        #endregion quote

        #region check
        /// <summary>
        /// STATUS - Command status
        /// </summary>
        /// <returns></returns>
        [Command("check", RunMode = RunMode.Async)]
        [Summary("Check a tweet")]
        [RequireOwner]
        [RequireContext(ContextType.DM)]
        public async Task Check(string url = "")
        {
            try
            {
                var id = Int64.Parse(url.Split('/')[url.Split('/').Length - 1]);
                var tweet = Tweet.GetTweet(id);
                var msg = TweetService.CheckTweetDetails(tweet, User.GetAuthenticatedUser());
                await ReplyAsync($"TWEET TESTS : ```{msg}```",deleteafter: TimeSpan.FromSeconds(10));
                Logger.Log(Logger.Warning, "Whoopsie", "Discord Check");
            }
            catch
            { Logger.Log(Logger.Warning, "Whoopsie", "Discord Check"); }
        }
        #endregion check

        #region role
        [Command("role", RunMode = RunMode.Async), Summary("Give the user the Photograph Role")]
        [Alias("photograph")]
        public async Task Role()
        {
            var user = Context.User as SocketGuildUser;
            var role = ((IGuildUser) user)?.Guild.Roles.FirstOrDefault(x => x.Name == "Photograph");
            if (user != null && !user.Roles.Contains(role))
            {
                var tweetUser = User.GetUserFromScreenName(user.Nickname);
                if (tweetUser != null)
                {
                    await user.AddRoleAsync(role);
                    await ReplyAsync("You're a Photograph now !");
                }
                else
                { await ReplyAsync("Can't find your Twitter Name. \nMake sure your Discord nickname and your Twitter @ are the same", deleteafter: TimeSpan.FromSeconds(5)); }
            }
            else
            { await ReplyAsync("You're already a Photograph !", deleteafter: TimeSpan.FromSeconds(5)); }
        }
        #endregion requirerole

        #region send
        [Command("send", RunMode = RunMode.Async), Summary("Send pictures to Twitter.")]
        [RequireRole("Photograph")]
        public async Task ImgSender()
        {
            List<IAttachment> attachments = new List<IAttachment>();
            var tempAttachments = new List<IAttachment>();
            if (Context.Message.Attachments.Count > 0)
            { attachments.Add(Context.Message.Attachments.FirstOrDefault()); }
            var delay = 15;
            var botMsg = await ReplyAsync($"Waiting for more images...\nYour message will be sent to Twitter in {delay} seconds");
            var typing = Context.Channel.EnterTypingState();
            for (int i = 0; i < delay; i++)
            {
                var startExec = DateTime.Now;
                var msgList = await Context.Channel.GetMessagesAsync(Context.Message, Direction.After, 10).FlattenAsync();
                var tempMsgs = msgList.Where(m => m.Author.Id == Context.Message.Author.Id
                                            && m.Attachments.Count > 0);

                if ((tempAttachments.Count + attachments.Count) < 4)
                {
                    tempAttachments.Clear(); // Clear temp attachments before filling it
                    foreach (var mess in tempMsgs)
                    {
                        if (mess.Attachments.Count > 0 && (tempAttachments.Count + attachments.Count) < 4)
                        { tempAttachments.Add(mess.Attachments.FirstOrDefault()); }
                    }
                }
                else
                { break; }
                var stopExec = DateTime.Now - startExec;
                if (stopExec.TotalMilliseconds < 1000)
                { await Task.Delay((int)(1000 - stopExec.TotalMilliseconds)); }
                await botMsg.ModifyAsync(m =>
                { m.Content = $"Waiting for more images...\nYour message will be sent to Twitter in {delay -1 - i} seconds"; });
            }
            attachments.AddRange(tempAttachments);
            await botMsg.DeleteAsync();
            var message = Context.Message.Content.Substring(Context.Message.Content.IndexOf("send", StringComparison.Ordinal) + "send".Length);
            var user = await Context.Guild.GetUserAsync(Context.User.Id);
            TweetService.DiscordTweet(message, user, attachments);
            typing.Dispose();
            await ReplyAsync($"Tweet sent with {attachments.Count} images !", deleteafter: TimeSpan.FromSeconds(3));
        }
        #endregion send

        #region loglvl
        [Command("loglvl"), Summary("Sets logs severity")]
        [RequireOwner]
        [RequireContext(ContextType.DM)]
        public async Task LogLevel(ushort log = 10)
        {
            try
            { await Context.Message.DeleteAsync(); }
            catch { /*ignored*/ }
            if (log <= 5)
            {
                Eva.LogLvl = log;
                await Context.User.SendMessageAsync($"Setting Log Level to {log}");
            }
            else
            { await Context.User.SendMessageAsync("Please enter a value between 0 (Critical Messages) and 5(Debug messages)"); }
        }
        #endregion

        #endregion COMMANDS
    }

    // Inherit from PreconditionAttribute
    public class RequireRoleAttribute : PreconditionAttribute
    {
        // Create a field to store the specified name
        private readonly string _name;

        // Create a constructor so the name can be specified
        public RequireRoleAttribute(string name) => _name = name;

        // Override the CheckPermissions method
        public override Task<PreconditionResult> CheckPermissionsAsync(ICommandContext context, CommandInfo command, IServiceProvider services)
        {
            // Check if this user is a Guild User, which is the only context where roles exist
            if (context.User is SocketGuildUser gUser)
            {
                // If this command was executed by a user with the appropriate role, return a success
                if (gUser.Roles.Any(r => r.Name == _name))
                    // Since no async work is done, the result has to be wrapped with `Task.FromResult` to avoid compiler errors
                    return Task.FromResult(PreconditionResult.FromSuccess());
                // Since it wasn't, fail
                else
                    return Task.FromResult(PreconditionResult.FromError($"You must have a role named {_name} to run this command."));
            }
            else
                return Task.FromResult(PreconditionResult.FromError("You must be in a guild to run this command."));
        }
    }
}
