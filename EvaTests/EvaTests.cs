using NUnit.Framework;
using Moq;
using Eva;
using Tweetinvi.Models;
using Tweetinvi.Models.Entities;
using System.Collections.Generic;
using Tweetinvi;
using System;

namespace Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            
            ITwitterCredentials creds = Eva.Eva.GetCredencials();
            Auth.SetCredentials(creds);
            Auth.ApplicationCredentials = creds;
        }

        #region MOCKS
        private Mock<ITweet> CreateMockTweet(List<string> medias, bool isRetweet, string text, IUser user, long? replyTo)
        {
            var Tweet = new Mock<ITweet>();
            List<IMediaEntity> mediaList = new List<IMediaEntity>();
            foreach (var str in medias)
            {
                mediaList.Add(null);
            }
            Tweet.Setup(t => t.Media).Returns(mediaList);
            Tweet.Setup(t => t.IsRetweet).Returns(isRetweet);
            Tweet.Setup(t => t.CreatedBy).Returns(user);
            Tweet.Setup(t => t.InReplyToStatusId).Returns(replyTo);
            return Tweet;
        }

        private Mock<IUser> CreateMockUser(string name, string screenName, string desc, long id)
        {
            var User = new Mock<IUser>();
            User.Setup(u => u.Name).Returns(name);
            User.Setup(u => u.ScreenName).Returns(screenName);
            User.Setup(u => u.Id).Returns(id);
            User.Setup(u => u.Description).Returns(desc);
            return User;
        }
        #endregion

        [Test]
        public void CheckTweetSentByBot()
        {
            IUser user = CreateMockUser("Eva", "EvaBot", "SomeDesc", 123456).Object;
            ITweet tweet = CreateMockTweet(new List<string> { "some media", "more medias" }, false, "Some Tweet", user, null).Object;
            bool value = Eva.Services.TweetService.CheckTweet(tweet, user);
            Assert.AreEqual(false, value);
        }

        [Test]
        public void CheckTweetSentIsRT()
        {
            IUser user = CreateMockUser("Eva", "EvaBot", "SomeDesc", 123456).Object;
            ITweet tweet = CreateMockTweet(new List<string> { "some media", "more medias" }, true, "Some Tweet", user, null).Object;
            bool value = Eva.Services.TweetService.CheckTweet(tweet, user);
            Assert.AreEqual(false, value);
        }

        [Test]
        public void CheckTweetSentIsReply()
        {
            IUser user = CreateMockUser("Eva", "EvaBot", "SomeDesc", 123456).Object;
            ITweet tweet = CreateMockTweet(new List<string> { "some media", "more medias" }, false, "Some Tweet", user, 123456).Object;
            bool value = Eva.Services.TweetService.CheckTweet(tweet, user);
            Assert.AreEqual(false, value);
        }

        [Test]
        public void CheckTweetSentNoMedia()
        {
            IUser user = CreateMockUser("Eva", "EvaBot", "SomeDesc", 123456).Object;
            ITweet tweet = CreateMockTweet(new List<string>(), false, "Some Tweet", user, null).Object;
            bool value = Eva.Services.TweetService.CheckTweet(tweet, user);
            Assert.AreEqual(false, value);
        }

        [Test]
        public void CheckTweetSentSuccess()
        {
            IUser user = CreateMockUser("Eva", "EvaBot", "SomeDesc", 123456).Object;
            ITweet tweet = CreateMockTweet(new List<string> { "some media", "more medias" }, false, "Some Tweet", user, null).Object;
            bool value = Eva.Services.TweetService.CheckTweet(tweet, User.GetAuthenticatedUser());
            Assert.AreEqual(true, value);
        }

    }
}