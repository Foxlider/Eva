using Discord;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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

        public static ITweet SendTweet(ITweet tweet)
        {
            string str = String.Format(startString[Eva.rand.Next(0, startString.Count)], tweet.CreatedBy.ScreenName, tweet.Media.Count);
            var response = Tweet.PublishTweet($"{str} https://twitter.com/{tweet.CreatedBy.ScreenName}/status/{tweet.Id}");
            foreach (var media in tweet.Media)
            {
                new Thread(() =>
                {
                    Thread.CurrentThread.Name = "MediaThread";
                    DownloadMedia(media, tweet.CreatedBy.ScreenName);
                }).Start();
            }
            return response;
        }

        public static ITweet DiscordTweet(string message, Discord.IUser user, List<IAttachment> attachments)
        {
            string str = String.Format(startString[Eva.rand.Next(0, startString.Count)], user.Username, attachments.Count);
            List<IMedia> medias = new List<IMedia>();
            List<Thread> threads = new List<Thread>();
            foreach (var media in attachments)
            {
                Thread t = new Thread(() =>
                {
                    var name = Eva.rand.Next(100, 999);
                    Log.Message(Log.info, $"Discord Media Thread {name} started", "Discord Media");
                    var t1 = DateTime.Now;
                    var str = DownloadMedia(media, user.Username);
                    var t2 = DateTime.Now;
                    var tweetMedia = Upload.UploadBinary(File.ReadAllBytes(str));
                    medias.Add(tweetMedia);
                    var t3 = DateTime.Now;
                    Log.Message(Log.info, $"Discord Media Thread {name} finished in {(t3-t1).TotalMilliseconds}ms \n" +
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

        private static void DownloadMedia(IMediaEntity media, string name)
        {
            using (WebClient client = new WebClient())
            {
                var ext = Path.GetExtension(media.MediaURLHttps);
                if (ext == ".jpg" || ext == ".png")
                {
                    var date = DateTime.Now;
                    if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "images", name)))
                        Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "images", name));
                    client.DownloadFile(new Uri(media.MediaURLHttps), Path.Combine(AppContext.BaseDirectory, "images", name, $"IMG_{name}_{date.ToString("dd_MM_yyyy_HH-mm-ss")}--{Eva.rand.Next(1000, 9999)}{ext}"));
                } 
            }
        }

        private static string DownloadMedia(IAttachment media, string name)
        {
            using (WebClient client = new WebClient())
            {
                var ext = Path.GetExtension(media.Url);
                var date = DateTime.Now;
                var path = Path.Combine(AppContext.BaseDirectory, "images", name, $"IMG_{name}_{date.ToString("dd_MM_yyyy_HH-mm-ss")}--{Eva.rand.Next(1000, 9999)}{ext}");
                if (ext == ".jpg" || ext == ".png")
                {
                    if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "images", name)))
                        Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "images", name));
                    client.DownloadFile(new Uri(media.Url), path);
                }
                return path;
            }
        }
    }
}
