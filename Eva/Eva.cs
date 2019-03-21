using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Eva.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
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
        private CommandService commands;
        public static DiscordSocketClient client;
        private IAuthenticationContext _authenticationContext;
        private IServiceProvider services;
        public static IConfigurationRoot Configuration;
        public static Random rand = new Random();
        internal static int logLvl = 3;

        static void Main(string[] args) => RunAsync(args).GetAwaiter().GetResult();

        public static async Task RunAsync(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
            var Eva = new Eva(args);
            await Eva.RunAsync();
        }

        public Eva(string[] args)
        {
            TryGenerateConfiguration();
            var builder = new ConfigurationBuilder()        // Create a new instance of the config builder
                .SetBasePath(AppContext.BaseDirectory)      // Specify the default location for the config file
                .AddJsonFile("config.json");                // Add this (json encoded) file to the configuration
            Configuration = builder.Build();                // Build the configuration

            IServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            services = serviceCollection.BuildServiceProvider();
        }

        private static bool TryGenerateConfiguration()
        {
            var filePath = Path.Combine(AppContext.BaseDirectory, "config.json");
            if (File.Exists(filePath))
            { return false; }
            object config = new EvaConfig();
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(filePath, json);
            return true;
        }

        /// <summary>
        /// MAIN LOOP
        /// </summary>
        /// <returns></returns>
        public async Task RunAsync()
        {
            Logger.Log(Logger.info, 
                $"Booting up...\n" +
                $"_________\n" +
                $"{Assembly.GetExecutingAssembly().GetName().Name} " +
                $"v{GetVersion()}\n" +
                $"_________\n", "Eva start");

            // Set up your credentials (https://apps.twitter.com)
            ITwitterCredentials creds = GetTwitterCredencials();
            Auth.SetCredentials(creds);
            Auth.ApplicationCredentials = creds;
            RateLimit.RateLimitTrackerMode = RateLimitTrackerMode.TrackAndAwait;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                                                 | SecurityProtocolType.Tls11
                                                 | SecurityProtocolType.Tls12;
            var authenticatedUser = User.GetAuthenticatedUser();
            Logger.Log(Logger.neutral,
                $"{authenticatedUser.Name} is connected :\n" +
                $"  _____________\n" +
                $"  ScreenName  : @{authenticatedUser.ScreenName}\n" +
                $"  Desc        : {authenticatedUser.Description.Replace("\n", "")}\n" +
                $"  Followers   : {authenticatedUser.FollowersCount}\n" +
                $"  _____________",
                "Eva start");

            client = new DiscordSocketClient(new DiscordSocketConfig
            { LogLevel = LogSeverity.Debug });
            client.Log += (LogMessage message) =>
            {
                Logger.Log((int)message.Severity, message.Message, message.Source);
                return Task.CompletedTask;
            };
            commands = new CommandService();
            await InstallCommands();

            await client.LoginAsync(TokenType.Bot,GetDiscordCredencials());
            await client.StartAsync();

            client.Ready += () =>
            {
                Logger.Log(Logger.neutral, $"{client.CurrentUser.Username}#{client.CurrentUser.Discriminator} is connected !\n\n" +
                    $"__[ CONNECTED TO ]__\n", "Eva Login");
                foreach (var guild in client.Guilds)
                {
                    Logger.Log(Logger.neutral,
                        $"\t_______________\n" +
                        $"\t{guild.Name} \n" +
                        $"\tOwned by {guild.Owner.Nickname}#{guild.Owner.Discriminator}\n" +
                        $"\t{guild.MemberCount} members", "Eva Login");
                }
                Logger.Log(Logger.neutral, "\t_______________", "Eva Login");
                Console.Title = $"{Assembly.GetExecutingAssembly().GetName().Name} v{GetVersion()}";
                SetDefaultStatus(client);
                return Task.CompletedTask;
            };

            var stream = Tweetinvi.Stream.CreateFilteredStream();

            // Thread2
            Thread t = new Thread(() =>
            {
                Thread.CurrentThread.Name = "StreamListener";
                var user = User.GetAuthenticatedUser();
                stream.AddTrack("EDPostcards");
                stream.AddTrack("ED_Postcards");
                stream.MatchingTweetReceived += (sender, args) =>
                {
                    var tweet = args.Tweet;
                    if (TweetService.CheckTweet(tweet, user))
                    {
                        Logger.Log(Logger.neutral, $"{tweet.CreatedBy.ScreenName}\n{tweet.Text}\n{tweet.Media.Count} media files", "StreamListener");
                        TweetService.SendTweet(tweet);
                    }
                };
                stream.StartStreamMatchingAnyCondition();

            });
            t.Start();

            t.Join();
            await Task.Delay(-1);
        }

        /// <summary>
        /// Install commands for the bot
        /// </summary>
        /// <returns></returns>
        public async Task InstallCommands()
        {
            // Hook the MessageReceived Event into our Command Handler
            client.MessageReceived += HandleCommand;
            // Discover all of the commands in this assembly and load them.
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), services);
        }

        /// <summary>
        /// Handle every Discord command
        /// </summary>
        /// <param name="messageParam"></param>
        /// <returns></returns>
        public async Task HandleCommand(SocketMessage messageParam)
        {
            // Don't process the command if it was a System Message
            if (!(messageParam is SocketUserMessage message))
            { return; }
            if (message.Channel is IPrivateChannel)
            { Logger.Log(Logger.neutral, $"{message.Author} in {message.Channel.Name}\n    :: { message.Content}", "DirectMessage"); }
            // Create a number to track where the prefix ends and the command begins
            int argPos = 0;
            if (!(message.HasStringPrefix(Configuration["prefix"], ref argPos) || message.HasMentionPrefix(client.CurrentUser, ref argPos)))
            { return; }
            // Create a Command Context
            var context = new CommandContext(client, message);
            // Execute the command. (result does not indicate a return value, 
            // rather an object stating if the command executed successfully)
            var result = await commands.ExecuteAsync(context, argPos, services);
            if (!result.IsSuccess)
            { await context.Channel.SendMessageAsync(result.ErrorReason); }
        }

        /// <summary>
        /// Get Credencials for Discord
        /// </summary>
        /// <returns>string Discord token</returns>
        public static string GetDiscordCredencials()
        {
            if (string.IsNullOrEmpty(Configuration["tokens:Discord"]))
            {
                Logger.Log(Logger.error, "Impossible to read Discord Token", "Eva Login");
                Logger.Log(Logger.neutral, "Do you want to edit the configuration file ? (Y/n)\n", "Eva Login");
                var answer = Console.ReadKey();
                if (answer.Key == ConsoleKey.Enter || answer.Key == ConsoleKey.Y)
                { EditDiscordToken(); }
                else
                {
                    Logger.Log(Logger.warning, "Shutting Down...\nPress Enter to continue.", "Eva Logout");
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
                Logger.Log(Logger.error, "Impossible to read Configuration.", "Eva Login");
                Logger.Log(Logger.neutral, "Do you want to edit the configuration file ? (Y/n)\n", "Eva Login");
                var answer = Console.ReadKey();
                if (answer.Key == ConsoleKey.Enter || answer.Key == ConsoleKey.Y)
                { EditTwitterToken(); }
                else
                {
                    Logger.Log(Logger.warning, "Shutting Down...\nPress Enter to continue.", "Eva Logout");
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

            Logger.Log(Logger.neutral, "Please enter the bot's token below.\n", "Eva Login");
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

            Logger.Log(Logger.neutral, "Please enter the bot's API key below.\n", "Eva Login");
            var answer = Console.ReadLine();
            Configuration["tokens:TwitterApiKey"] = answer.Trim();

            Logger.Log(Logger.neutral, "Please enter the bot's API secret below.\n", "Eva Login");
            answer = Console.ReadLine();
            Configuration["tokens:TwitterApiSecret"] = answer.Trim();

            Logger.Log(Logger.neutral, "Please enter the bot's Access Token below.\n", "Eva Login");
            answer = Console.ReadLine();
            Configuration["tokens:TwitterToken"] = answer.Trim();

            Logger.Log(Logger.neutral, "Please enter the bot's Token Secret below.\n", "Eva Login");
            answer = Console.ReadLine();
            Configuration["tokens:TwitterTokenSecret"] = answer.Trim();

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
        public static async Task SetDefaultStatus(DiscordSocketClient client)
        {
            await client.SetGameAsync($"#EDPostcards :: E.V.A. v{GetVersion()}", type: ActivityType.Watching);
        }

        /// <summary>
        /// Get current version
        /// </summary>
        /// <returns></returns>
        public static string GetVersion()
        {
            string rev = "b";
#if DEBUG
            rev = "a";
#endif
            return $"{Assembly.GetExecutingAssembly().GetName().Version.Major}.{Assembly.GetExecutingAssembly().GetName().Version.Minor}{rev}";
        }

        /// <summary>
        /// Shutdown
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            var latestException = ExceptionHandler.GetLastException();
            if(latestException != null)
            {
                Logger.Log(Logger.error, $"ERROR : [{latestException.StatusCode}] {latestException.TwitterDescription}\n{latestException.TwitterExceptionInfos} ");
            }
            try
            { client.LogoutAsync(); }
            catch { }
            finally
            { client.Dispose(); }
            Logger.Log(Logger.warning, "Shutting Down...", "Eva Logout");
            Environment.Exit(0);
        }
    }


    internal class EvaConfig
    {
        public string Prefix { get; set; }
        public Tokens Tokens { get; set; }

        public EvaConfig(string prefix = "!!", Tokens token = null)
        {
            this.Prefix = prefix;
            this.Tokens = token;
        }
    }

    internal class Tokens
    {
        public string Discord { get; set; }
        public string TwitterApiKey { get; set; }
        public string TwitterApiSecret { get; set; }
        public string TwitterToken { get; set; }
        public string TwitterTokenSecret { get; set; }

        public Tokens(string Discord = null, 
                      string TwitterApiKey = null, 
                      string TwitterApiSecret = null,
                      string TwitterToken = null,
                      string TwitterTokenSecret =null)
        {
            this.Discord = Discord;
            this.TwitterApiKey = TwitterApiKey;
            this.TwitterApiSecret = TwitterApiSecret;
            this.TwitterToken = TwitterToken;
            this.TwitterTokenSecret = TwitterTokenSecret;
        }
    }
}
