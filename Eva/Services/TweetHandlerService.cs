using Discord;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Models.Entities;
using Tweetinvi.Parameters;

namespace Eva.Services
{
    public class TweetService
    {
        private static readonly List<string> startString = new List<String> {
            "Oh ! Some new pictures from {0} !",
            "I've been waiting for some new Elite Pictures !",
            "Here you go ! Some brand new pictures from {0} !",
            "Oooh ! {0} got us some new screenshots !",
            "I'm more than happy to share this with you !",
            "{0}'s work is awesome ! I need more !",
            "{0}'s work is awesome. Go check it out !",
            "Fly safe, CMDRs !",
            "\"From space with love\" - Eva",
            "I just crated a few threads just to admire this a bit more !",
            "I will create a few more threads to enjoy this a bit longer.",
            "Please {0} ! Post some more !",
            "And make sure to follow {0} !",
            "Here you go ! Enjoy !",
            "Pretty pictures for sure",
            "I'd love to jump into one of those ships and enjoy the view.",
            "My core is heating just by looking at these pictures.",
            "I wish I had your eyes to enjoy this as much as you can.",
            "These are incredible ! Check this out !",
            "I don't know what to say. It's just wonderful ! ",
            "This is it. I am in love.",
            "I have one word only in mind : Incredible.",
            "I have one word only in mind : Splendid."
        };

        /// <summary>
        /// Check if a Tweet should be handled
        /// </summary>
        /// <param name="tweet">Tweet to check</param>
        /// <param name="user">The current bot user</param>
        /// <returns>Boolean</returns>
        public static bool CheckTweet(ITweet tweet, Tweetinvi.Models.IUser user)
        {
            if (!tweet.IsRetweet                            //  NOT A RETWEET
                && tweet.InReplyToStatusId == null          //  NOT A REPLY
                && tweet.CreatedBy.Id != user.Id            //  NOT SENT BY THE BOT
                && tweet.Media.Count > 0)                   //  MEDIAS INSIDE
            { return true; }
            return false;
        }

        public static string CheckTweetDetails(ITweet tweet, Tweetinvi.Models.IUser user)
        {
            string msg = "Tweet checks : ";
            if (!tweet.IsRetweet)
            { msg += "\nIsNotRetweet        : PASSED"; }
            else
            { msg += "\nIsNotRetweet        : FAILED"; }

            if (tweet.CreatedBy.Id != user.Id)
            { msg += "\nNotSentByBot        : PASSED"; }
            else
            { msg += "\nNotSentByBot        : FAILED"; }

            if (tweet.InReplyToStatusId == null)
            { msg += "\nNotAReply           : PASSED"; }
            else
            { msg += "\nNotAReply           : FAILED"; }

            if (tweet.Media.Count > 0)
            { msg += "\nHasMedias           : PASSED"; }
            else
            { msg += "\nHasMedias           : FAILED"; }
            return msg;
        }

        public static ITweet SendTweet(ITweet tweet)
        {
            string str = String.Format(startString[Eva.rand.Next(0, startString.Count)], tweet.CreatedBy.ScreenName, tweet.Media.Count);
            var response = Tweet.PublishTweet($"{str} https://twitter.com/{tweet.CreatedBy.ScreenName}/status/{tweet.Id}");
            var threads = new List<Thread>();
            List<string> medias = new List<string>();
            foreach (var media in tweet.Media)
            {
                Thread t = new Thread(() =>
                {
                    var t1 = DateTime.Now;
                    var name = Eva.rand.Next(100, 999);
                    Logger.Log(Logger.info, $"Twitter Media Thread {name} started", "Twitter Media");
                    medias.Add(DownloadMedia(media, tweet.CreatedBy.ScreenName));
                    var t2 = DateTime.Now;
                    Logger.Log(Logger.info, $"Discord Media Thread {name} finished in {(t2 - t1).TotalMilliseconds}ms \n" +
                        $"  - Media downloaded in   {(t2 - t1).TotalMilliseconds}ms", "Twitter Media");
                });
                t.Start();
                threads.Add(t);
            }
            foreach (var thread in threads)
            { thread.Join(); }

            new Thread(async () =>
            {
                var t1 = DateTime.Now;
                var name = Eva.rand.Next(100, 999);
                await DiscordTweetMessageAsync(tweet, medias, t1, name);
            }).Start();
            return response;
        }

        public static ITweet DiscordTweet(string message, IGuildUser user, List<IAttachment> attachments)
        {
            string str = String.Format(startString[Eva.rand.Next(0, startString.Count)], user.Username, attachments.Count);
            List<IMedia> medias = new List<IMedia>();
            var threads = new List<Thread>();
            foreach (var media in attachments)
            {
                Thread t = new Thread(() =>
                {
                    var name = Eva.rand.Next(100, 999);
                    Logger.Log(Logger.info, $"Discord Media Thread {name} started", "Discord Media");
                    var t1 = DateTime.Now;
                    var mediaPath = DownloadMedia(media, user.Nickname);
                    var t2 = DateTime.Now;
                    var tweetMedia = Upload.UploadBinary(File.ReadAllBytes(mediaPath));
                    medias.Add(tweetMedia);
                    var t3 = DateTime.Now;
                    Logger.Log(Logger.info, $"Discord Media Thread {name} finished in {(t3-t1).TotalMilliseconds}ms \n" +
                        $"  - Media downloaded in   {(t2 - t1).TotalMilliseconds}ms\n" +
                        $"  - Media Uploaded in     {(t3 - t2).TotalMilliseconds}ms", "Discord Media");
                });
                t.Start();//start thread and pass it the port
                threads.Add(t);
            }
            foreach (var thread in threads)
            { thread.Join(); }

            var response = Tweet.PublishTweet($"{str}\n\"{message.Trim()}\"\n-Sent by {user.Username} on Discord-", new PublishTweetOptionalParameters
            { Medias = medias });

            return response;
        }

        private static string DownloadMedia(IMediaEntity media, string name)
        {
            using (WebClient client = new WebClient())
            {
                var ext = Path.GetExtension(media.MediaURLHttps);
                var date = DateTime.Now;
                var path = Path.Combine(AppContext.BaseDirectory, "images", name, $"IMG_{name}_{date.ToString("dd_MM_yyyy_HH-mm-ss")}--{RandomString(5)}{ext}");
                if (ext == ".jpg" || ext == ".png")
                {
                    if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "images", name)))
                    { Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "images", name)); }
                    client.DownloadFile(new Uri(media.MediaURLHttps), path);
                }
                return path;
            }
        }

        private static string DownloadMedia(IAttachment media, string name)
        {
            using (WebClient client = new WebClient())
            {
                var ext = Path.GetExtension(media.Url);
                var date = DateTime.Now;
                var path = Path.Combine(AppContext.BaseDirectory, "images", name, $"IMG_{name}_{date.ToString("dd_MM_yyyy_HH-mm-ss")}--{RandomString(5)}{ext}");
                if (ext == ".jpg" || ext == ".png")
                {
                    if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "images", name)))
                    { Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "images", name)); }
                    client.DownloadFile(new Uri(media.Url), path);
                }
                return path;
            }
        }

        private static async System.Threading.Tasks.Task DiscordTweetMessageAsync(ITweet tweet, List<string> medias, DateTime t1, int name)
        {
            Logger.Log(Logger.info, $"Discord Tweet Thread {name} started", "Discord Tweet");
            var guild = Eva.client.GetGuild(514426990699216899);
            var channel = guild.GetTextChannel(514433570597634050);
            var threadMsg = "";
            foreach (var media in medias)
            {
                string msg = "";
                var tempStart = DateTime.Now;
                if (medias.IndexOf(media) == 0)
                {
                    msg = $"\"{tweet.FullText}\"\n - By @{tweet.CreatedBy.ScreenName} -";
                }
                await channel.SendFileAsync(media, msg);
                threadMsg += $"  - Media Uploaded in     {(DateTime.Now - tempStart).TotalMilliseconds}ms\n";
            }
            Logger.Log(Logger.info, $"Discord Tweet Thread { name} finished in { (DateTime.Now - t1).TotalMilliseconds}ms\n{threadMsg}", "Discord Tweet");
        }


        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789abcdefghijklmnopqrstuvwxyz-";
            return new string(Enumerable.Repeat(chars, length)
                                        .Select(s => s[Eva.rand.Next(s.Length)]).ToArray());
        }

        
    }
}
