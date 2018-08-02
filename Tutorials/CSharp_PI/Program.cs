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
        public int Id;
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
        static System.Timers.Timer _timer;
        static IngressClient _client;

        private static string Sanitize(string raw)
        {
            return Regex.Replace(raw, @"(@[A-Za-z0-9]+)|([^0-9A-Za-z \t])|(\w+:\/\/\S+)", " ").ToString();
        }

        private static DateTime QuantizeTime (DateTime dt)
        {
            TimeSpan d = new TimeSpan(0, 1, 0); // 1 minute
            var delta = dt.Ticks % d.Ticks;
            return new DateTime(dt.Ticks - delta, dt.Kind);
        }

        private static void Setup()
        {
            // Send Type messages (both Static and Dynamic)
            _client.CreateTypes(new string[] { ProviderType.JsonSchema, TrackType.JsonSchema });
            _client.CreateTypes(new string[] { StatType.JsonSchema });

            // Send Container messages
            List<Container> streams = new List<Container>();
            string containerId;
            foreach (TrackStat stat in _stats)
            {
                containerId = String.Format("Container{0}", stat.Id);
                streams.Add (new Container { Id = containerId, TypeId = "StatType" });
            }

            _client.CreateContainers(streams);

            // Send Assets in Data messages
            AssetLinkValues<ProviderType> assetParent = new AssetLinkValues<ProviderType>()
            {
                typeid = "ProviderType",
                Values = new List<ProviderType> { new ProviderType()
                    {  Index = "Provider0", Name = "Twitter" }
                }
            };
            _client.SendValuesAsync(new AssetLinkValues<ProviderType>[] { assetParent }).Wait();

            List<TrackType> tracks = new List<TrackType>();
            foreach (TrackStat stat in _stats)
            {
                tracks.Add(new TrackType { Index = String.Format("Track{0}", stat.Id), Name = stat.TrackValue });
            }

            AssetLinkValues<TrackType> assetChild = new AssetLinkValues<TrackType>()
            {
                typeid = "TrackType",
                Values = tracks
            };
            _client.SendValuesAsync(new AssetLinkValues<TrackType>[] { assetChild }).Wait();

            // Send Asset-to-child-Asset Links
            List<AFLink<StaticElement, StaticElement>> list1 = new List<AFLink<StaticElement, StaticElement>>();

            list1.Add(new AFLink<StaticElement, StaticElement>() {
                source = new StaticElement() { typeid = "ProviderType", index = "_ROOT" },
                target = new StaticElement() { typeid = "ProviderType", index = "Provider0" }
            });

            foreach (TrackStat stat in _stats)
            {
                list1.Add(new AFLink<StaticElement, StaticElement>()
                {
                    source = new StaticElement() { typeid = "ProviderType", index = "Provider0" },
                    target = new StaticElement() { typeid = "TrackType", index = String.Format("Track{0}", stat.Id ) }
                });
            }

            AssetLinkValues<AFLink<StaticElement, StaticElement>> dataLink = new AssetLinkValues<AFLink<StaticElement, StaticElement>>()
            {
                typeid = "__Link",
                Values = list1
            };
            _client.SendValuesAsync(new AssetLinkValues<AFLink<StaticElement, StaticElement>>[] { dataLink, }).Wait();

            // Send Asset-to-Data (i.e. Dynamic Attribute) Links
            List<AFLink<StaticElement, DynamicElement>> list2 = new List<AFLink<StaticElement, DynamicElement>>();

            foreach (TrackStat stat in _stats)
            {
                list2.Add(new AFLink<StaticElement, DynamicElement>()
                {
                    source = new StaticElement() { typeid = "TrackType", index = String.Format ("Track{0}", stat.Id) },
                    target = new DynamicElement() { containerid = String.Format ("Container{0}", stat.Id) }
                });
            }
            AssetLinkValues<AFLink<StaticElement, DynamicElement>> dynamic_dataLink = new AssetLinkValues<AFLink<StaticElement, DynamicElement>>()
            {
                typeid = "__Link",
                Values = list2 
            };
            _client.SendValuesAsync(new AssetLinkValues<AFLink<StaticElement, DynamicElement>>[] { dynamic_dataLink }).Wait();
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

            //_stream.AddTrack("california fire");
            //_stream.AddTrack("green fish");

            List<string> tracks = jsonValues.Tracks.ToObject<List<string>>();

            int id = 1;
            foreach (string track in tracks)
            {
                _stats.Add (new TrackStat
                {
                    Id = id++,
                    TrackValue = track
                });
                _stream.AddTrack(track);
            }
            _periodStart = QuantizeTime(DateTime.Now);

            _stream.MatchingTweetReceived += (sender, tweetArgs) =>
            {
                //CheckTime();    // need to do this on a timer

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

            _stream.UnmanagedEventReceived += (sender, eventArgs) => {
                Console.WriteLine("unmanaged event received");
            };
            _stream.DisconnectMessageReceived += (sender, disconnectedArgs) =>
            {
                Console.WriteLine("received disconnected message");
                //Console.WriteLine(disconnectedArgs.DisconnectMessage);
            };

            _stream.JsonObjectReceived += (sender, jsonArgs) =>
            {
                //Console.WriteLine(jsonArgs.Json);
            };

            _stream.KeepAliveReceived += (sender, eventArgs) => {
                Console.WriteLine("received keep alive");
            };

            _stream.LimitReached += (sender, eventArgs) => {
                Console.WriteLine("limit reached");
            };

            _stream.WarningFallingBehindDetected += (sender, eventArgs) => {
                Console.WriteLine("fall behind warning");
            };


            // create Http client to send requests to ingress service
            _client = new IngressClient(ingressServiceUrl, producerToken);

            // Use compression when sending data.
            _client.UseCompression = jsonValues.compressionGzip;

            Setup();


            /*
                        // Send Type messages (both Static and Dynamic)
                        _client.CreateTypes(new string[] { FirstStaticType.JsonSchema, SecondStaticType.JsonSchema } );
                        _client.CreateTypes(new string[] { FirstDynamicType.JsonSchema, SecondDynamicType.JsonSchema, ThirdDynamicType.JsonSchema });

                        // Send Container messages
                        Container[] streams = {
                            new Container() { Id = "Container1", TypeId = "FirstDynamicType" },
                            new Container() { Id = "Container2", TypeId = "FirstDynamicType" },
                            new Container() { Id = "Container3", TypeId = "SecondDynamicType"},
                            new Container() { Id = "Container4", TypeId = "ThirdDynamicType" }
                        };
                        _client.CreateContainers(streams);

                        // Send Assets in Data messages
                        AssetLinkValues<FirstStaticType> assetParent = new AssetLinkValues<FirstStaticType>()
                        {
                            typeid = "FirstStaticType",
                            Values = new List<FirstStaticType> { new FirstStaticType()
                                {  index="Asset1", name="Parent element", StringProperty="Parent element attribute value"  }
                            }
                        };
                        _client.SendValuesAsync(new AssetLinkValues<FirstStaticType>[] { assetParent}).Wait();

                        AssetLinkValues<SecondStaticType> assetChild = new AssetLinkValues<SecondStaticType>()
                        {
                            typeid = "SecondStaticType",
                            Values = new List<SecondStaticType> { new SecondStaticType()
                                { index="Asset2", name="Child element", StringProperty="Child element attribute value"  }
                            }
                        };
                        _client.SendValuesAsync(new AssetLinkValues<SecondStaticType>[] { assetChild }).Wait();

                        // Send Asset-to-child-Asset Links
                        AssetLinkValues<AFLink<StaticElement,StaticElement>> dataLink = new AssetLinkValues<AFLink<StaticElement, StaticElement>>()
                        {
                            typeid = "__Link",
                            Values = new List<AFLink<StaticElement, StaticElement>>   {
                                new AFLink<StaticElement,StaticElement>() {source = new StaticElement() {typeid = "FirstStaticType", index = "_ROOT" }, target = new StaticElement() {typeid= "FirstStaticType", index= "Asset1" } },
                                new AFLink<StaticElement,StaticElement>() {source = new StaticElement() {typeid = "FirstStaticType", index = "Asset1" }, target = new StaticElement() {typeid= "SecondStaticType", index= "Asset2" } }
                            }
                        };
                        _client.SendValuesAsync(new AssetLinkValues<AFLink<StaticElement, StaticElement>>[] { dataLink, }).Wait();

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
                        _client.SendValuesAsync(new AssetLinkValues<AFLink<StaticElement, DynamicElement>>[] { dynamic_dataLink }).Wait();
            */

            // Setting handler for Ctrl+C to exit sending data loop
            bool continueRunning = true;
            AutoResetEvent exitEvent = new AutoResetEvent(false);

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                continueRunning = false;
                Console.Write("Stopping... ");
                eventArgs.Cancel = true;
                _stream.StopStream();
                exitEvent.Set();
            };

            _timer = new System.Timers.Timer(5000)
            {
                AutoReset = false
            };

            _timer.Elapsed += (s, e) => CheckTime();
            _timer.Start();

            _stream.StartStreamMatchingAnyCondition();

            exitEvent.WaitOne();

            return;
/*
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
            */
        }

        private static void _stream_DisconnectMessageReceived(object sender, Tweetinvi.Events.DisconnectedEventArgs e)
        {
            throw new NotImplementedException();
        }

        static void CheckTime()
        {
            DateTime dt = QuantizeTime(DateTime.Now);
            Console.WriteLine("Checking time at {0}", dt.ToLongTimeString());

            if (dt > _periodStart)
            {
                List<DataValues> dataValues = new List<DataValues>();

                foreach (TrackStat stat in _stats)
                {
                    List<StatType> values = new List<StatType>();
                    values.Add(new StatType()
                    {
                        TimeStamp = _periodStart,
                        ItemCount = stat.TweetCount
                    });
                    dataValues.Add(new DataValues
                    {
                        ContainerId = String.Format ("Container{0}", stat.Id),
                        Values = values
                    });
                    Console.WriteLine("time {0}, send {1} = {2}", _periodStart.ToLongTimeString(), stat.TrackValue, stat.TweetCount);
                    stat.TweetCount = 0;
                }

                // Now send them
                _client.SendValuesAsync(dataValues).Wait();

                _periodStart = dt;
            }

            _timer.Enabled = true;
        }
    }
}
