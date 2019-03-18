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
        public static bool CheckTweet(ITweet tweet, IUser user)
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

        private static void DownloadMedia(IMediaEntity media, string name)
        {
            //https://stackoverflow.com/questions/8349693/how-to-check-if-a-byte-array-is-a-valid-image
            using (WebClient client = new WebClient())
            {
                Log.Message(Log.neutral, Path.Combine(AppContext.BaseDirectory, "images", name), "Media Thread");
                var date = DateTime.Now;
                if (!Directory.Exists(Path.Combine(AppContext.BaseDirectory, "images", name)))
                    Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "images", name));
                client.DownloadFile(new Uri(media.MediaURLHttps), Path.Combine(AppContext.BaseDirectory, "images", name, $"IMG_{name}_{date.ToString("dd_MM_yyyy_HH-mm-ss")}--{Eva.rand.Next(1000, 9999)}.jpg"));
            }
                
        }
    }
}
