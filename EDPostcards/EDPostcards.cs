using System;
using System.Reflection;
using System.Threading;
using Tweetinvi;
using Tweetinvi.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EDPostcards.Services;
using System.Threading.Tasks;

namespace EDPostcards
{
    public class EDPostcards
    {
        private IAuthenticationContext _authenticationContext;
        private IServiceProvider services;
        internal static int logLvl = 3;

        static void Main(string[] args) => RunAsync(args).GetAwaiter().GetResult();

        public static async Task RunAsync(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(CurrentDomain_ProcessExit);
            var EDPostcards = new EDPostcards(args);
            await EDPostcards.RunAsync();
        }

        public EDPostcards(string[] args)
        {
            IServiceCollection serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            services = serviceCollection.BuildServiceProvider();
        }

        /// <summary>
        /// MAIN LOOP
        /// </summary>
        /// <returns></returns>
        public async Task RunAsync()
        {
            Log.Message(Log.info, 
                $"Booting up...\n" +
                $"____________\n" +
                $"{Assembly.GetExecutingAssembly().GetName().Name} " +
                $"v{GetVersion()}\n" +
                $"____________\n", "EDPostcard start");

            // Set up your credentials (https://apps.twitter.com)
            ITwitterCredentials creds = new TwitterCredentials("kpdduMJ1YIuXdanBYZddw8Z0f",
                "Do1MRHot62rVIWK5YbkzlaG5ffS09IB0keV7Lvq99yvYDu5wDT",
                "852893965858820101-5NlnPbgMqHxbjG4EOHHp01cVYgENIqW",
                "TtjyVtDlIiqTYgwyCe3DiNU9zdBmZpRwSm7tCkCgT97Q8");
            Auth.SetCredentials(creds);
            Auth.ApplicationCredentials = creds;
            RateLimit.RateLimitTrackerMode = RateLimitTrackerMode.TrackAndAwait;

            var authenticatedUser = User.GetAuthenticatedUser();
            Log.Message(Log.info,
                $"{authenticatedUser.Name} is connected :\n" +
                $"  _____________\n" +
                $"  ScreenName  : @{authenticatedUser.ScreenName}\n" +
                $"  Desc        : {authenticatedUser.Description.Replace("\n", "")}\n" +
                $"  Followers   : {authenticatedUser.FollowersCount}\n" +
                $"  _____________",
                "EDPostcard start");

            var settings = authenticatedUser.GetAccountSettings();
            // Publish the Tweet "Hello World" on your Timeline
            //Tweet.PublishTweet("I'LL BE BACK");

            var stream = Stream.CreateFilteredStream();

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
                    if (!tweet.IsRetweet                            //  NOT A RETWEET
                        && tweet.InReplyToStatusId == null          //  NOT A REPLY
                        && tweet.CreatedBy.Id != user.Id            //  NOT SENT BY THE BOT
                        && tweet.Media.Count > 0)                   //  MEDIAS INSIDE
                    { Log.Message(Log.neutral, $"{tweet.CreatedBy.ScreenName}\n{tweet.Text}\n{tweet.Media.Count} media files", Thread.CurrentThread.Name); }
                    Log.Message(Log.neutral, "A tweet containing 'ED Postcards' has been found; the tweet is '" + args.Tweet + "'", Thread.CurrentThread.Name);
                };
                stream.StartStreamMatchingAnyCondition();

            });
            t.Start();

            t.Join();
            await Task.Delay(-1);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Log.Message(Log.warning, "Shutting Down...", "EDPostcards Logout");
            Environment.Exit(0);
        }

        /// <summary>
        /// Get services set up
        /// </summary>
        /// <param name="serviceCollection"></param>
        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(new TweetHandlerService());
            serviceCollection.AddSingleton(new LoggingService());
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
    }
}
