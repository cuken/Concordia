﻿using System;
using Concordia.Audio;
using DiscordSharp;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using DiscordSharp.Objects;
using System.IO;
using Newtonsoft.Json;
using Concordia.Managers;

namespace Concordia
{
    class Concordia
    {
        public static DiscordClient client;
        public static DiscordMember owner;
        public static DiscordMember bot;
        public static AudioPlayer audioPlayer = new AudioPlayer(new DiscordVoiceConfig { Bitrate = null, Channels = 1, FrameLengthMs = 60, OpusMode = Discord.Audio.Opus.OpusApplication.LowLatency, SendOnly = false });
        public static WaitHandle waitHandle = new AutoResetEvent(false);        
        CancellationToken cancelToken;
        DateTime loginDate;
        public static Config.Config config;
        public static DiscordVoiceClient voice = new DiscordVoiceClient(client);


        static void Main(string[] args) => new Concordia().Start(args);        


        private void Start(string[] args)
        {
            Console.Title = $"Concordia Discord Bot";
            cancelToken = new CancellationToken();

            if (File.Exists("settings.json"))
                config = JsonConvert.DeserializeObject<Config.Config>(File.ReadAllText("settings.json"));
            else
                config = new Config.Config();
            if (config.CommandPrefix.ToString().Length == 0)
                config.CommandPrefix = "!";

            client = new DiscordClient();
            client.RequestAllUsersOnStartup = true;

            client.ClientPrivateInformation.email = config.BotEmail;
            client.ClientPrivateInformation.password = config.BotPass;
            SetupEvents(cancelToken);

            Console.ReadLine();

        }
                
        private Task SetupEvents(CancellationToken cancelToken)
        {
            return Task.Run(() =>
            {

                client.MessageReceived += (sender, e) =>
                {
                    Console.WriteLine($"[Message from {e.author.Username} in {e.Channel.Name} on {e.Channel.parent.name}]: {e.message.content} ");
                    MessageManager.Instance.AddMessageToQue(e);            

                };
                //client.VoiceClientConnected += (sender, e) =>
                //{
                //    //owner.SlideIntoDMs($"Voice connection complete.");
                //    //bufferedWaveProvider = new BufferedWaveProvider(waveFormat);
                //    //bufferedWaveProvider.BufferDuration = new TimeSpan(0, 0, 50);
                //    //volumeProvider = new VolumeWaveProvider16(bufferedWaveProvider);
                //    //volumeProvider.Volume = 1.1f;
                //    //outputDevice.Init(volumeProvider);

                //    //stutterReducingTimer = new System.Timers.Timer(500);
                //    //stutterReducingTimer.Elapsed += StutterReducingTimer_Elapsed;
                //    //PlayAudioAsync(cancelToken);
                //};

                //client.AudioPacketReceived += (sender, e) =>
                //{
                //    if (bufferedWaveProvider != null)
                //    {
                //        byte[] potential = new byte[4000];
                //        int decodedFrames = client.GetVoiceClient().Decoder.DecodeFrame(e.OpusAudio, 0, e.OpusAudioLength, potential);
                //        bufferedWaveProvider.AddSamples(potential, 0, decodedFrames);
                //    }
                //};

                client.GuildCreated += (sender, e) =>
                {
                    //owner.SlideIntoDMs($"[Joined Server]: {e.server.name}");
                };
                client.SocketClosed += (sender, e) =>
                {
                    if (e.Code != 1000 && !e.WasClean)
                    {
                        Console.WriteLine($"Socket Closed! Code: {e.Code}. Reason: {e.Reason}. Clear: {e.WasClean}.");
                        Console.WriteLine("Waiting 6 seconds to reconnect..");
                        Thread.Sleep(6 * 1000);
                        client.Connect();
                    }
                    else
                    {
                        Console.WriteLine($"Shutting down ({e.Code}, {e.Reason}, {e.WasClean})");
                    }

                };
                client.TextClientDebugMessageReceived += (sender, e) =>
                {
                    if (e.message.Level == MessageLevel.Error || e.message.Level == MessageLevel.Critical)
                    {
                        Helper.WriteError($"(Logger Error) {e.message.Message}");
                        try
                        {
                            owner.SlideIntoDMs($"Bot error ocurred: ({e.message.Level.ToString()})```\n{e.message.Message}\n```");
                        }
                        catch { }
                    }
                    if (e.message.Level == MessageLevel.Warning)
                        Helper.WriteWarning($"(Logger Warning) {e.message.Message}");
                };
                client.Connected += (sender, e) =>
                {
                    bot = e.user;
                    Console.WriteLine("Connected as " + e.user.Username);
                    loginDate = DateTime.Now;

                };
                if (client.SendLoginRequest() != null)
                {
                    client.Connect();
                }
            }, cancelToken);
        }

        public void Exit()
        {
            client.Logout();
            client.Dispose();
            Environment.Exit(0);
        }

    }
}
