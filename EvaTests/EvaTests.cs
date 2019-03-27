using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using Tweetinvi;
using Tweetinvi.Models;
using Tweetinvi.Models.Entities;

namespace EvaTests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            var builder = new ConfigurationBuilder()        // Create a new instance of the config builder
                .SetBasePath(AppContext.BaseDirectory)      // Specify the default location for the config file
                .AddJsonFile("config.json");                // Add this (json encoded) file to the configuration
            Eva.Eva.Configuration = builder.Build();                // Build the configuration
            ITwitterCredentials creds = Eva.Eva.GetTwitterCredencials();
            Auth.SetCredentials(creds);
            Auth.ApplicationCredentials = creds;
        }

        #region MOCKS
        private Mock<ITweet> CreateMockTweet(List<string> medias, bool isRetweet, string text, IUser user, long? replyTo)
        {
            var tweet = new Mock<ITweet>();
            List<IMediaEntity> mediaList = new List<IMediaEntity>();
            foreach (var _ in medias)
            { mediaList.Add(null); }
            tweet.Setup(t => t.Media).Returns(mediaList);
            tweet.Setup(t => t.IsRetweet).Returns(isRetweet);
            tweet.Setup(t => t.CreatedBy).Returns(user);
            tweet.Setup(t => t.Text).Returns(text);
            tweet.Setup(t => t.InReplyToStatusId).Returns(replyTo);
            return tweet;
        }

        private Mock<IUser> CreateMockUser(string name, string screenName, string desc, long id)
        {
            var user = new Mock<IUser>();
            user.Setup(u => u.Name).Returns(name);
            user.Setup(u => u.ScreenName).Returns(screenName);
            user.Setup(u => u.Id).Returns(id);
            user.Setup(u => u.Description).Returns(desc);
            return user;
        }
        #endregion

        [Test]
        public void CheckTweetSentByBot()
        {
            IUser user = CreateMockUser("Eva", "EvaBot", "SomeDesc", 123456).Object;
            ITweet tweet = CreateMockTweet(new List<string> { "some media", "more medias" }, false, "Some Tweet", user, null).Object;
            Assert.AreEqual(false, Eva.Services.TweetService.CheckTweet(tweet, user));
        }

        [Test]
        public void CheckTweetSentIsRt()
        {
            IUser user = CreateMockUser("Eva", "EvaBot", "SomeDesc", 123456).Object;
            ITweet tweet = CreateMockTweet(new List<string> { "some media", "more medias" }, true, "Some Tweet", user, null).Object;
            Assert.AreEqual(false, Eva.Services.TweetService.CheckTweet(tweet, user));
        }

        [Test]
        public void CheckTweetSentIsReply()
        {
            IUser user = CreateMockUser("Eva", "EvaBot", "SomeDesc", 123456).Object;
            ITweet tweet = CreateMockTweet(new List<string> { "some media", "more medias" }, false, "Some Tweet", user, 123456).Object;
            Assert.AreEqual(false, Eva.Services.TweetService.CheckTweet(tweet, user));
        }

        [Test]
        public void CheckTweetSentNoMedia()
        {
            IUser user = CreateMockUser("Eva", "EvaBot", "SomeDesc", 123456).Object;
            ITweet tweet = CreateMockTweet(new List<string>(), false, "Some Tweet", user, null).Object;
            Assert.AreEqual(false, Eva.Services.TweetService.CheckTweet(tweet, user));
        }

        [Test]
        public void CheckTweetSentSuccess()
        {
            IUser user = CreateMockUser("Eva", "EvaBot", "SomeDesc", 123456).Object;
            ITweet tweet = CreateMockTweet(new List<string> { "some media", "more medias" }, false, "Some Tweet", user, null).Object;
            Assert.AreEqual(true, Eva.Services.TweetService.CheckTweet(tweet, User.GetAuthenticatedUser()));
        }

    }
}