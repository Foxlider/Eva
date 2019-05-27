using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Eva.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Tweetinvi;
using Tweetinvi.Models;

namespace Eva
{
    public class Eva
    {
        private CommandService _commands;
        public static DiscordSocketClient Client;
        private readonly IServiceProvider _services;
        public static IConfigurationRoot Configuration;
        public static readonly Random Rand = new Random();
        internal static int LogLvl = 3;
        private static bool _breaker;

        static void Main(string[] args)
        {
            try
            {
                RunAsync(args).GetAwaiter().GetResult();
                var latestException = ExceptionHandler.GetLastException();
                if (latestException != null)
                {
                    Logger.Log(Logger.Error, $"ERROR : [{latestException.StatusCode}] {latestException.TwitterDescription}\n{latestException.TwitterExceptionInfos} ");
                }
                Logger.Log(Logger.Info, "EVA Exiting", "Main");
            }
            catch (Exception e)
            {
                var latestException = ExceptionHandler.GetLastException();
                if (latestException != null)
                {
                    Logger.Log(Logger.Error, $"ERROR : [{latestException.StatusCode}] {latestException.TwitterDescription}\n{latestException.TwitterExceptionInfos} ");
                }
                Logger.Log(Logger.Error, $"An error occured : {e.Message}\nSOURCE : {e.Source}\nTRIGGERED BY : {e.InnerException?.Message}\nSTACKTRACE{e.StackTrace}", "Main");
            }
        }

        private static async Task RunAsync(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExitAsync;
            var eva = new Eva(args);
            await eva.RunAsync();
        }

        private Eva(string[] args)
        {
            TryGenerateConfiguration();
            var builder = new ConfigurationBuilder()        // Create a new instance of the config builder
                .SetBasePath(AppContext.BaseDirectory)      // Specify the default location for the config file
                .AddJsonFile("config.json");                // Add this (json encoded) file to the configuration
            Configuration = builder.Build();                // Build the configuration

            IServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            _services = serviceCollection.BuildServiceProvider();
            foreach (var arg in args)
            { Logger.Log(Logger.Debug, $"{arg}", "Main"); }
        }

        private static void TryGenerateConfiguration()
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "config.json");
            if (File.Exists(filePath)) { return; }
            object config = new EvaConfig();
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// MAIN LOOP
        /// </summary>
        /// <returns></returns>
        private async Task RunAsync()
        {
            string version = $"{Assembly.GetExecutingAssembly().GetName().Name} v{GetVersion()}";

            Logger.Log(Logger.Info,
                       $"Booting up...\n"
                     + $"┌─{new string('─', version.Length)}─┐\n"
                     + $"│ {version} │\n"
                     + $"└─{new string('─', version.Length)}─┘\n", "Eva start");
            Console.Title = $@"{Assembly.GetExecutingAssembly().GetName().Name} v{GetVersion()}";
            List<Thread> threadList = new List<Thread>();

            Thread twitterThread = new Thread(TwitterThread)
            { Name = "TwitterThread" };
            Thread discordThread = new Thread(DiscordThread)
            { Name = "DiscordThread" };
            threadList.Add(twitterThread);
            threadList.Add(discordThread);
            foreach (var thread in threadList)
            {
                thread.Start();
                if (thread.ThreadState == System.Threading.ThreadState.Running)
                { Logger.Log(Logger.Info, $"Thread {thread.Name ?? ""} started."); }
                
            }
            twitterThread.Join();
            Logger.Log(Logger.Info, "Twitter exited");
            while (!_breaker)
            {
                await Task.Delay(60000);
                if (twitterThread.IsAlive) continue;
                Logger.Log(Logger.Warning, $"Thread {twitterThread.Name} exited. Restarting...", "ThreadWatcher");
                var ex = ExceptionHandler.GetLastException();
                Logger.Log(Logger.Warning, $"{ex?.StatusCode} :{ex?.TwitterDescription} ");
                twitterThread = new Thread(TwitterThread);
                try { twitterThread.Start(); }
                catch (Exception e)
                {
                    Logger.Log(Logger.Warning, $"Could not Start {twitterThread.Name} : {e.Message}", "ThreadWatcher");
                    twitterThread.Join();
                }
            }
        }

        private static void TwitterThread(object callback)
        {
            var thread = Thread.CurrentThread;
            // Set up your credentials (https://apps.twitter.com)
            var creds = GetTwitterCredencials();
            Auth.SetCredentials(creds);
            Auth.ApplicationCredentials = creds;
            RateLimit.RateLimitTrackerMode = RateLimitTrackerMode.TrackAndAwait;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                 | SecurityProtocolType.Tls11
                                                 | SecurityProtocolType.Tls12;
            var authenticatedUser = User.GetAuthenticatedUser();
            try
            {
                Logger.Log(Logger.Neutral,
                       $"{authenticatedUser.Name} is connected :\n"
                     + $"  ┌──────────────\n"
                     + $"  │ ScreenName  : @{authenticatedUser.ScreenName}\n"
                     + $"  │ Desc        : {authenticatedUser.Description.Replace("\n", "")}\n"
                     + $"  │ Followers   : {authenticatedUser.FollowersCount}\n"
                     + $"  └──────────────",
                thread.Name ?? "");
            }
            catch (Exception e)
            {
                var latestException = ExceptionHandler.GetLastException();
                Logger.Log(Logger.Error, $"Impossible to log in. Check logs for more details. {e.Message}\n" +
                $"The following error occured : '{latestException?.TwitterDescription?? "NO EXCEPTION HANDLED BY TWEEETINVI"}'", thread.Name ?? "");
                Environment.Exit(1);
            }

            var streamThread = new Thread(StreamThread)
            { Name = "StreamThread" };
            streamThread.Start();
            streamThread.Join();
            Logger.Log(Logger.Info, $"Thread {thread.Name} stopping : StreamThread exited.");
        }

        private async void DiscordThread(object callback)
        {
            var thread = Thread.CurrentThread;
            Client = new DiscordSocketClient(new DiscordSocketConfig
            { LogLevel = LogSeverity.Debug });
            Client.Log += message =>
            {
                Logger.Log((int)message.Severity, message.Message, message.Source);
                return Task.CompletedTask;
            };
            
            _commands = new CommandService();
            _commands.Log += message =>
            {
                Logger.Log((int)message.Severity, message.Message, message.Source);
                return Task.CompletedTask;
            };
            await InstallCommands();

            await Client.LoginAsync(TokenType.Bot, GetDiscordCredencials());
            await Client.StartAsync();

            Client.Ready += async () =>
            {
                Logger.Log(Logger.Neutral, $"{Client.CurrentUser.Username}#{Client.CurrentUser.Discriminator} is connected !\n\n__[ CONNECTED TO ]__\n  ┌─", thread.Name ?? "EvaLogin");
                foreach (var guild in Client.Guilds)
                {
                    Logger.Log(Logger.Neutral,
                               $"  │┌───────────────\n"
                             + $"  ││ {guild.Name} \n"
                             + $"  ││ Owned by {guild.Owner.Nickname}#{guild.Owner.Discriminator}\n"
                             + $"  ││ {guild.MemberCount} members\n"
                             + $"  │└───────────────", thread.Name ?? "EvaLogin");
                }           
                Logger.Log(Logger.Neutral, "  └─", thread.Name ?? "EvaLogin");
                await SetDefaultStatus(Client);
                //return Task.CompletedTask;
            };
        }

        private static async void StreamThread(object callback)
        {
            Logger.Log(Logger.Info, $"Thread {Thread.CurrentThread.Name} started", Thread.CurrentThread.Name);
            var stream = Tweetinvi.Stream.CreateFilteredStream();
            var user   = User.GetAuthenticatedUser();
            stream.AddTrack("EDPostcards");
            stream.AddTrack("ED_Postcards");

            stream.MatchingTweetReceived += (sender, args) =>
            {
                Logger.Log(Logger.Info, "Received tweet matching filters.");
                var tweet = args.Tweet;
                if (!TweetService.CheckTweet(tweet, user)) return;
                Logger.Log(Logger.Neutral, $" [ TWEETING ] \n{tweet.CreatedBy.ScreenName}\n{tweet.Text}\n{tweet.Media.Count} media files", "StreamListener");
                TweetService.SendTweet(tweet);
            };
            stream.StreamStopped += (sender, args) =>
            {
                stream.StartStreamMatchingAllConditions();
            };
            stream.StartStreamMatchingAnyCondition();
            await Task.Delay(-1);
        }

        /// <summary>
        /// Install commands for the bot
        /// </summary>
        /// <returns></returns>
        private async Task InstallCommands()
        {
            // Discover all of the commands in this assembly and load them.
            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            
            _commands.CommandExecuted += OnCommandExecuteAsync;
            
            // Hook the MessageReceived Event into our Command Handler
            Client.MessageReceived += HandleCommand;
        }

        private static async Task OnCommandExecuteAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
        {
            // We have access to the information of the command executed,
            // the context of the command, and the result returned from the
            // execution in this event.
            var argPos = 0;
            if (!(context.Message.HasStringPrefix(Configuration["prefix"], ref argPos) || context.Message.HasMentionPrefix(Client.CurrentUser, ref argPos) || context.Message.Author.IsBot))
            { return; }
            var commandName = command.IsSpecified ? command.Value.Name : "A command";
            // We can tell the user what went wrong
            if (!string.IsNullOrEmpty(result?.ErrorReason))
            {
                Logger.Log(new LogMessage(LogSeverity.Warning, "CMD Execution", $"{commandName} was called and failed : {result.ErrorReason}."));
                var msg = await context.Channel.SendMessageAsync(result.ErrorReason);
                Thread.Sleep(2000);
                await msg.DeleteAsync();
            }
            Logger.Log(new LogMessage(LogSeverity.Info, "CMD Execution", $"{commandName} was executed."));
        }

        /// <summary>
        /// Handle every Discord command
        /// </summary>
        /// <param name="messageParam"></param>
        /// <returns></returns>
        private async Task HandleCommand(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            if (!(messageParam is SocketUserMessage message))
            { return; }

            //Log every private message received
            if (message.Channel is IPrivateChannel)
            { Logger.Log(Logger.Neutral, $"{message.Author} in {message.Channel.Name}\n{"└─".PadLeft(message.Author.ToString().Length-3)}{ message.Content}", "DirectMessage"); }
            
            // Create a number to track where the prefix ends and the command begins
            var argPos = 0;
            if (!(message.HasStringPrefix(Configuration["prefix"], ref argPos))) { return; }
            if (message.HasMentionPrefix(Client.CurrentUser, ref argPos)) { return; }
            if (message.Author.IsBot) { return; }

            // Create a Command Context
            var context = new CommandContext(Client, message);
            await _commands.ExecuteAsync(context, argPos, _services);
        }

        /// <summary>
        /// Get Credencials for Discord
        /// </summary>
        /// <returns>string Discord token</returns>
        private static string GetDiscordCredencials()
        {
            if (string.IsNullOrEmpty(Configuration["tokens:Discord"]))
            {
                Logger.Log(Logger.Error, "Impossible to read Discord Token", "Eva Login");
                Logger.Log(Logger.Neutral, "Do you want to edit the configuration file ? (Y/n)\n", "Eva Login");
                var answer = Console.ReadKey();
                if (answer.Key == ConsoleKey.Enter || answer.Key == ConsoleKey.Y)
                { EditDiscordToken(); }
                else
                {
                    Logger.Log(Logger.Warning, "Shutting Down...\nPress Enter to continue.", "Eva Logout");
                    Console.ReadKey();
                    Environment.Exit(-1);
                }
            }
            return Configuration["tokens:Discord"];
        }

        /// <summary>
        /// Get Credencials for twitter
        /// </summary>
        /// <returns>ITwitterCredencials to login</returns>
        public static ITwitterCredentials GetTwitterCredencials()
        {
            if (string.IsNullOrEmpty(Configuration["tokens:TwitterApiKey"]) 
                || string.IsNullOrEmpty(Configuration["tokens:TwitterApiSecret"])
                || string.IsNullOrEmpty(Configuration["tokens:TwitterToken"])
                || string.IsNullOrEmpty(Configuration["tokens:TwitterTokenSecret"]))
            {
                Logger.Log(Logger.Error, "Impossible to read Configuration.", "Eva Login");
                Logger.Log(Logger.Neutral, "Do you want to edit the configuration file ? (Y/n)\n", "Eva Login");
                var answer = Console.ReadKey();
                if (answer.Key == ConsoleKey.Enter || answer.Key == ConsoleKey.Y)
                { EditTwitterToken(); }
                else
                {
                    Logger.Log(Logger.Warning, "Shutting Down...\nPress Enter to continue.", "Eva Logout");
                    Console.ReadKey();
                    Environment.Exit(-1);
                }
            }
            return new TwitterCredentials(Configuration["tokens:TwitterApiKey"],
                Configuration["tokens:TwitterApiSecret"],
                Configuration["tokens:TwitterToken"],
                Configuration["tokens:TwitterTokenSecret"]);
        }

        /// <summary>
        /// Editing Discord Configuration Token
        /// </summary>
        private static void EditDiscordToken()
        {
            string url = "https://discordapp.com/developers/applications/514397114835533829/bots";
            try
            { Process.Start(url); }
            catch
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                { Process.Start("xdg-open", url); }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                { Process.Start("open", url); }
                else
                { throw; }
            }

            Logger.Log(Logger.Neutral, "Please enter the bot's token below.\n", "Eva Login");
            var answer = Console.ReadLine();
            Configuration["tokens:discord"] = answer;
            var filePath = Path.Combine(AppContext.BaseDirectory, "config.json");
            object config = new EvaConfig(Configuration["prefix"], new Tokens(Configuration["tokens:Discord"],
                                                                              Configuration["tokens:TwitterApiKey"],
                                                                              Configuration["tokens:TwitterApiSecret"],
                                                                              Configuration["tokens:TwitterToken"],
                                                                              Configuration["tokens:TwitterTokenSecret"]));
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.Delete(filePath);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Editing Twitter Configuration Tokens
        /// </summary>
        private static void EditTwitterToken()
        {
            string url = "https://developer.twitter.com/en/apps/13667962";
            try
            { Process.Start(url); }
            catch
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                { Process.Start("xdg-open", url); }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                { Process.Start("open", url); }
                else
                { throw; }
            }

            Logger.Log(Logger.Neutral, "Please enter the bot's API key below.\n", "Eva Login");
            var answer = Console.ReadLine();
            Configuration["tokens:TwitterApiKey"] = answer?.Trim();

            Logger.Log(Logger.Neutral, "Please enter the bot's API secret below.\n", "Eva Login");
            answer = Console.ReadLine();
            Configuration["tokens:TwitterApiSecret"] = answer?.Trim();

            Logger.Log(Logger.Neutral, "Please enter the bot's Access Token below.\n", "Eva Login");
            answer = Console.ReadLine();
            Configuration["tokens:TwitterToken"] = answer?.Trim();

            Logger.Log(Logger.Neutral, "Please enter the bot's Token Secret below.\n", "Eva Login");
            answer = Console.ReadLine();
            Configuration["tokens:TwitterTokenSecret"] = answer?.Trim();

            var filePath = Path.Combine(AppContext.BaseDirectory, "config.json");
            object config = new EvaConfig(Configuration["prefix"], new Tokens(Configuration["tokens:Discord"], 
                                                                              Configuration["tokens:TwitterApiKey"],
                                                                              Configuration["tokens:TwitterApiSecret"],
                                                                              Configuration["tokens:TwitterToken"],
                                                                              Configuration["tokens:TwitterTokenSecret"]));
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.Delete(filePath);
            File.WriteAllText(filePath, json);
            Configuration.Reload();
        }

        /// <summary>
        /// Get services set up
        /// </summary>
        /// <param name="serviceCollection"></param>
        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(new TweetService());
            serviceCollection.AddSingleton(new Logger());
        }

        /// <summary>
        /// Setting current status
        /// </summary>
        private static async Task SetDefaultStatus(DiscordSocketClient client)
        {
            await client.SetGameAsync($"#EDPostcards :: E.V.A. v{GetVersion()}", type: ActivityType.Watching);
        }

        /// <summary>
        /// Get current version
        /// </summary>
        /// <returns></returns>
        public static string GetVersion()
        {
            string rev = $"{(char)(Assembly.GetExecutingAssembly().GetName().Version.Build + 97)}";
        #if !DEBUG
            rev += "-r";
        #endif
            return $"{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}{rev}";
        }

        /// <summary>
        /// Shutdown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static async void CurrentDomain_ProcessExitAsync(object sender, EventArgs e)
        {
            _breaker = true;
            var latestException = ExceptionHandler.GetLastException();
            if(latestException != null)
            {
                Logger.Log(Logger.Error, $"ERROR : [{latestException.StatusCode}] {latestException.TwitterDescription}\n{latestException.TwitterExceptionInfos} ");
            }
            try
            { await Client.LogoutAsync(); }
            finally
            { Client.Dispose(); }
            Logger.Log(Logger.Warning, "Shutting Down...", "Eva Logout");
            Environment.Exit(0);
        }
    }

    internal class EvaConfig
    {
        public string Prefix { get; set; }
        public Tokens Tokens { get; set; }

        public EvaConfig(string prefix = "!!", Tokens token = null)
        {
            Prefix = prefix;
            Tokens = token;
        }
    }

    internal class Tokens
    {
        public string Discord { get; set; }
        public string TwitterApiKey { get; set; }
        public string TwitterApiSecret { get; set; }
        public string TwitterToken { get; set; }
        public string TwitterTokenSecret { get; set; }

        public Tokens(string discord = null, 
                      string twitterApiKey = null, 
                      string twitterApiSecret = null,
                      string twitterToken = null,
                      string twitterTokenSecret =null)
        {
            Discord = discord;
            TwitterApiKey = twitterApiKey;
            TwitterApiSecret = twitterApiSecret;
            TwitterToken = twitterToken;
            TwitterTokenSecret = twitterTokenSecret;
        }
    }
}
