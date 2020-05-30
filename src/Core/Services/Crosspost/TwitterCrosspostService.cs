using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tweetinvi;

namespace Core.Services.Crosspost
{
    public class TwitterCrosspostService : ICrossPostService
    {
        private static readonly Semaphore Semaphore = new Semaphore(1, 1);

        private readonly ILogger _logger;
        private readonly string _consumerKey;
        private readonly string _consumerSecret;
        private readonly string _accessToken;
        private readonly string _accessTokenSecret;
        private readonly string _name;
        private readonly IReadOnlyCollection<string> _defaultTags;

        private const int MaxTweetLength = 186;

        public TwitterCrosspostService(
            string consumerKey, 
            string consumerSecret,
            string accessToken,
            string accessTokenSecret,
            string name,
            IReadOnlyCollection<string> defaultTags,
            ILogger<TwitterCrosspostService> logger)
        {
            _logger = logger;
            _defaultTags = defaultTags;
            _consumerKey = consumerKey;
            _consumerSecret = consumerSecret;
            _accessToken = accessToken;
            _accessTokenSecret = accessTokenSecret;
            _name = name;
        }

        public async Task Send(string message, Uri link, [NotNull] IReadOnlyCollection<string> tags)
        {
            var tagLine = string.Join(" ", _defaultTags.ToList().Union(tags));

            var maxMessageLength = MaxTweetLength - tagLine.Length;

            var text= $"{Substring(message, maxMessageLength)} {tagLine} {link}";
            
            try
            {
                Semaphore.WaitOne();

                Auth.SetUserCredentials(_consumerKey, _consumerSecret, _accessToken, _accessTokenSecret);

                var publishTweetParameters = Tweet.CreatePublishTweetParameters(text);
                
                Tweet.PublishTweet(publishTweetParameters);
                
                _logger.LogInformation($"Message was sent to Twitter channel `{_name}`: `{text}`");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TwitterCrosspostService.Send");
            }
            finally
            {
                Semaphore.Release();
            }
        }

        private static string Substring(string text, int length)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;

            return text.Length <= length ? text : $"{text.Substring(0, length - 4)}... ";
        }
    }
}