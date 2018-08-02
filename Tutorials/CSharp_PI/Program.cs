/*************************************************************************************
# Copyright 2018 OSIsoft, LLC
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
# <http://www.apache.org/licenses/LICENSE-2.0>
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# NOTE: this script was designed using the v1.0
# version of the OMF specification, as outlined here:
# http://omf-docs.osisoft.com/en/v1.0
# For more info, see OMF Developer Companion Guide:
# http://omf-companion-docs.osisoft.com
****************************************************************************************/

using IngressServiceAPI.API;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using Tweetinvi;
using Tweetinvi.Streaming;
using Tweetinvi.Models;
using System.Text.RegularExpressions;
//using SimpleNetNlp;

namespace IngressServiceAPI
{
    class TrackStat
    {
        public string TrackValue;
        public int TweetCount;
    }

    class Program
    {
        static string consumerKey;
        static string consumerSecret;
        static string accessToken;
        static string accessTokenSecret;
        static IFilteredStream _stream;
        static DateTime _periodStart;
        static List<TrackStat> _stats = new List<TrackStat>();

        private static string Sanitize(string raw)
        {
            return Regex.Replace(raw, @"(@[A-Za-z0-9]+)|([^0-9A-Za-z \t])|(\w+:\/\/\S+)", " ").ToString();
        }

        static void Main(string[] args)
        {
            // get and parse json configuration file
            string jsonConfig        = File.ReadAllText(Directory.GetCurrentDirectory() + "/config.json");            
            dynamic jsonValues       = JObject.Parse(jsonConfig);
            string ingressServiceUrl = jsonValues.endpoint; 
            string producerToken     = jsonValues.producertoken;
            int delayInterval     = jsonValues.interval;

            consumerKey = jsonValues.ConsumerKey;
            consumerSecret = jsonValues.ConsumerSecret;
            accessToken = jsonValues.AccessToken;
            accessTokenSecret = jsonValues.AccessTokenSecret;

            Auth.SetUserCredentials(consumerKey, consumerSecret, accessToken, accessTokenSecret);

            _stream = Tweetinvi.Stream.CreateFilteredStream();
            _stream.AddTweetLanguageFilter(LanguageFilter.English);
            _stream.AddTrack("california fire");
            //_stream.AddTrack("green fish");

            
            _stream.MatchingTweetReceived += (sender, tweetArgs) =>
            {
                CheckTime();

                var sanitized = Sanitize(tweetArgs.Tweet.FullText);
                //var sentence = new Sentence(sanitized);

                //Console.WriteLine(tweetArgs.Tweet.CreatedBy.Name);
                Console.WriteLine(tweetArgs.Tweet.Text);
                Console.WriteLine(String.Join(";", tweetArgs.MatchingTracks));

                TrackStat stat;
                foreach (string track in tweetArgs.MatchingTracks)
                {
                    stat = _stats.Where(x => x.TrackValue == track).FirstOrDefault();
                    if (stat != null)
                        ++stat.TweetCount;
                }

                //Console.WriteLine(sentence.Sentiment);
            };

            _stream.StartStreamMatchingAnyCondition();

            // create Http client to send requests to ingress service
            IngressClient client = new IngressClient(ingressServiceUrl, producerToken);

            // Use compression when sending data.
            client.UseCompression = jsonValues.compressionGzip;

            // Send Type messages (both Static and Dynamic)
            client.CreateTypes(new string[] { FirstStaticType.JsonSchema, SecondStaticType.JsonSchema } );
            client.CreateTypes(new string[] { FirstDynamicType.JsonSchema, SecondDynamicType.JsonSchema, ThirdDynamicType.JsonSchema });

            // Send Container messages
            Container[] streams = {
                new Container() { Id = "Container1", TypeId = "FirstDynamicType" },
                new Container() { Id = "Container2", TypeId = "FirstDynamicType" },
                new Container() { Id = "Container3", TypeId = "SecondDynamicType"},
                new Container() { Id = "Container4", TypeId = "ThirdDynamicType" }
            };
            client.CreateContainers(streams);

            // Send Assets in Data messages
            AssetLinkValues<FirstStaticType> assetParent = new AssetLinkValues<FirstStaticType>()
            {
                typeid = "FirstStaticType",
                Values = new List<FirstStaticType> { new FirstStaticType()
                    {  index="Asset1", name="Parent element", StringProperty="Parent element attribute value"  }
                }
            };
            client.SendValuesAsync(new AssetLinkValues<FirstStaticType>[] { assetParent}).Wait();

            AssetLinkValues<SecondStaticType> assetChild = new AssetLinkValues<SecondStaticType>()
            {
                typeid = "SecondStaticType",
                Values = new List<SecondStaticType> { new SecondStaticType()
                    { index="Asset2", name="Child element", StringProperty="Child element attribute value"  }
                }
            };
            client.SendValuesAsync(new AssetLinkValues<SecondStaticType>[] { assetChild }).Wait();

            // Send Asset-to-child-Asset Links
            AssetLinkValues<AFLink<StaticElement,StaticElement>> dataLink = new AssetLinkValues<AFLink<StaticElement, StaticElement>>()
            {
                typeid = "__Link",
                Values = new List<AFLink<StaticElement, StaticElement>>   {
                    new AFLink<StaticElement,StaticElement>() {source = new StaticElement() {typeid = "FirstStaticType", index = "_ROOT" }, target = new StaticElement() {typeid= "FirstStaticType", index= "Asset1" } },
                    new AFLink<StaticElement,StaticElement>() {source = new StaticElement() {typeid = "FirstStaticType", index = "Asset1" }, target = new StaticElement() {typeid= "SecondStaticType", index= "Asset2" } }
                }
            };
            client.SendValuesAsync(new AssetLinkValues<AFLink<StaticElement, StaticElement>>[] { dataLink, }).Wait();

            // Send Asset-to-Data (i.e. Dynamic Attribute) Links
            AssetLinkValues<AFLink<StaticElement, DynamicElement>> dynamic_dataLink = new AssetLinkValues<AFLink<StaticElement, DynamicElement>>()
            {
                typeid = "__Link",
                Values = new List<AFLink<StaticElement, DynamicElement>>   {
                    new AFLink<StaticElement, DynamicElement>(){ source = new StaticElement() { typeid = "FirstStaticType", index= "Asset1" },target= new DynamicElement() { containerid ="Container1" } },
                    new AFLink<StaticElement, DynamicElement>(){ source = new StaticElement() { typeid = "SecondStaticType", index= "Asset2" },target= new DynamicElement() { containerid ="Container2" } },
                    new AFLink<StaticElement, DynamicElement>(){ source = new StaticElement() { typeid = "SecondStaticType", index= "Asset2" },target= new DynamicElement() { containerid ="Container3" } },
                    new AFLink<StaticElement, DynamicElement>(){ source = new StaticElement() { typeid = "SecondStaticType", index= "Asset2" },target= new DynamicElement() { containerid ="Container4" } }
                }
            };
            client.SendValuesAsync(new AssetLinkValues<AFLink<StaticElement, DynamicElement>>[] { dynamic_dataLink }).Wait();

            // Setting handler for Ctrl+C to exit sending data loop
            bool continueRunning = true;
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                continueRunning = false;
                Console.Write("Stopping... ");
                eventArgs.Cancel = true;
                _stream.StopStream();
            };

            // simulate realtime data
            Random rint = new Random();
            Random rdouble = new Random();
            string string_boolean_value = "True";
            int    integer_enum_value = 0;

            // Now send simulated relatime data continously
            while (continueRunning)
            {
                // Create set of integers to send to streams
                List<FirstDynamicType> values = new List<FirstDynamicType>();
                for(int i = 0; i < 3; i++)
                {
                    values.Add(new FirstDynamicType() { timestamp = DateTime.UtcNow, IntegerProperty = rint.Next() });
                    Thread.Sleep(10);  // Offset the time-stamps by 10 ms
                }
                DataValues vals1 = new DataValues() { ContainerId = streams[0].Id, Values = values };
                DataValues vals2 = new DataValues() { ContainerId = streams[1].Id, Values = values };
                // Now send them
                //client.SendValuesAsync(new DataValues[] { vals1, vals2 }).Wait();
           
                // Create set of SecondDynamicType values to send to streams
                List<SecondDynamicType> fnumbers = new List<SecondDynamicType>();
                for (int i = 0; i < 3; i++)
                {
                    string_boolean_value = (string_boolean_value == "True") ? "False" : "True";
                    fnumbers.Add(new SecondDynamicType() { timestamp = DateTime.UtcNow, NumberProperty1 = rdouble.NextDouble(), NumberProperty2= rdouble.NextDouble(), StringEnum = string_boolean_value });
                    Thread.Sleep(10);  // Offset the time-stamps by 10 ms
                }
                DataValues nums = new DataValues() { ContainerId = streams[2].Id, Values = fnumbers };
                //client.SendValuesAsync(new DataValues[] { nums }).Wait();


                // Create set of ThirdDynamicType values to send to streams
                List<ThirdDynamicType> enumvalues = new List<ThirdDynamicType>();
                for (int i = 0; i < 3; i++)
                {
                    integer_enum_value = (integer_enum_value == 0) ? 1 : 0;
                    enumvalues.Add(new ThirdDynamicType() { timestamp = DateTime.UtcNow,  IntegerEnum = integer_enum_value });
                    Thread.Sleep(10);  // Offset the time-stamps by 10 ms
                }
                DataValues bvals = new DataValues() { ContainerId = streams[3].Id, Values = enumvalues };
                //client.SendValuesAsync(new DataValues[] { bvals }).Wait();

                Thread.Sleep(delayInterval);
            }
        }

        static void CheckTime()
        {

        }
    }
}
