
using Bandwidth.Standard;
using Bandwidth.Standard.Utilities;
using Bandwidth.Standard.Voice.Bxml;
using Bandwidth.Standard.Voice.Models;

using Bandwidth.Standard.Messaging.Models;

using VoiceController = Bandwidth.Standard.Voice.Controllers.APIController;
using MessagingController = Bandwidth.Standard.Messaging.Controllers.APIController;
using Helpers;
using System;

using static Eagle.Server;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Text;
using System.IO;
using Bandwidth.Standard.Http.Client;

namespace RecordingExample
{
    class RecordingExample
    {
        private static string voiceUsername = Environment.GetEnvironmentVariable("VOICE_API_USERNAME");
        private static string voicePassword = Environment.GetEnvironmentVariable("VOICE_API_PASSWORD");

        private static string msgUser = Environment.GetEnvironmentVariable("MSG_API_USERNAME");
        private static string msgPassword = Environment.GetEnvironmentVariable("MSG_API_PASSWORD");
        private static string msgApplicationId = Environment.GetEnvironmentVariable("MSG_APPLICATION_ID");
        private static string msgAccountId = Environment.GetEnvironmentVariable("MSG_ACCOUNT_ID");

        private static Configuration config = new Configuration().ToBuilder()
                .WithVoiceBasicAuthPassword(voicePassword)
                .WithVoiceBasicAuthUserName(voiceUsername)
                .WithMessagingBasicAuthPassword(msgPassword)
                .WithMessagingBasicAuthUserName(msgUser)
                .WithEnvironment(Configuration.Environments.PRODUCTION)
                .Build();

        private static string voiceAccountId = Environment.GetEnvironmentVariable("VOICE_ACCOUNT_ID");

        private static string voiceServer = Environment.GetEnvironmentVariable("VOICE_SERVER");

        private static BandwidthClient clientInit = new BandwidthClient(config);

        private static VoiceController voiceController = clientInit.Voice.APIController;
        private static MessagingController msgController = clientInit.Messaging.APIController;

        static void Main(string[] args)
        {

            //Setup Call initiated callback URL
            // in the bandwidth console.  https://dashboard.bandwidth.com/  


            //Starting the Eagle Server 
            useHttp(true);
            port("8080");
            startServerInstance();

            //Set listneing at the path "incoming/call"
            //Executes the following lambda function when a request is recieved.
            post("/incoming/call", (request, response) => {

                string json = ControllerHelpers.getBody(request);
                dynamic callback = APIHelper.JsonDeserialize<dynamic>(json);
                Response bxmlResponse = new Response();

                switch ((string)callback.direction)
                {
                    case "inbound":
                        switch ((string)callback.eventType)
                        {
                            case "initiate":
                                {
                                    var playAudio = new PlayAudio();
                                    playAudio.Url = "https://www.kozco.com/tech/piano2.wav";

                                    var speakSentence = new SpeakSentence();
                                    speakSentence.Sentence = "Please, leave a voicemail after the beep and press #, beep.";
                                    speakSentence.Voice = "julie";

                                    var redirect = new Redirect();
                                    redirect.RedirectUrl = "/record/start";

                                    bxmlResponse.Add(playAudio);
                                    bxmlResponse.Add(speakSentence);
                                    bxmlResponse.Add(redirect);
                                }
                                break;
                            default:
                                bxmlResponse.Add(new Hangup()); //All others hangup.
                                break;
                        }
                        break;
                    default:
                        bxmlResponse.Add(new Hangup()); //We will not be makeing outboud calls.
                        break;
                }

                return bxmlResponse.ToBXML();
            });

            post("/record/check", (request, response) => {

                string json = ControllerHelpers.getBody(request);
                dynamic callback = APIHelper.JsonDeserialize<dynamic>(json);
                Response bxmlResponse = new Response();

                switch ((string)callback.direction)
                {
                    case "inbound":
                        switch ((string)callback.eventType)
                        {
                            case "redirect":
                                {
                                    var speakSentence = new SpeakSentence();
                                    speakSentence.Sentence = "If you'd like to hear your recording, press 1 followed by #, otherwise please hangup";
                                    speakSentence.Voice = "julie";

                                    var gather = new Gather();
                                    gather.TerminatingDigits = "#";
                                    gather.GatherUrl = "/record/check";
                                    gather.Tag = callback.Tag;
                                    gather.SpeakSentence = speakSentence;

                                    bxmlResponse.Add(gather);
                                }
                                break;
                            case "gather":
                                string digits = callback.digits;
                                string recordingUrl = callback.tag;
                                if (digits.Equals("1"))
                                {

                                    var playAudio = new PlayAudio();
                                    playAudio.Url = recordingUrl;
                                    playAudio.Username = voiceUsername;
                                    playAudio.Password = voicePassword;

                                    var speakSentence = new SpeakSentence();
                                    speakSentence.Sentence = "If you'd like to record again, press 1 followed by #, otherwise please hangup";
                                    speakSentence.Voice = "julie";

                                    var gather = new Gather();
                                    gather.TerminatingDigits = "#";
                                    gather.GatherUrl = "/record/start";
                                    gather.SpeakSentence = speakSentence;
                                    gather.Tag = recordingUrl;

                                    bxmlResponse.Add(playAudio);
                                    bxmlResponse.Add(gather);

                                }
                                else
                                {
                                    var speakSentence = new SpeakSentence();
                                    speakSentence.Sentence = "Thank you, a text message of the recording has been sent ";
                                    speakSentence.Voice = "julie";

                                    bxmlResponse.Add(speakSentence);

                                    sendRecordingAsMMSMessage(recordingUrl, "+19192347322", (string)callback.from);

                                    Console.WriteLine("Call Finished");

                                }
                                break;
                            default:
                                bxmlResponse.Add(new Hangup()); //All others hangup.
                                break;
                        }
                        break;
                    default:
                        bxmlResponse.Add(new Hangup()); //We will not be makeing outboud calls.
                        break;
                }

                return bxmlResponse.ToBXML();
            });

            post("/record/start", (request, response) => {

                string json = ControllerHelpers.getBody(request);
                dynamic callback = APIHelper.JsonDeserialize<dynamic>(json);
                Response bxmlResponse = new Response();

                switch ((string)callback.direction)
                {
                    case "inbound":
                        switch ((string)callback.eventType)
                        {
                            case "redirect":
                                {
                                    var record = new Record();
                                    record.RecordCompleteUrl = "/record/callbacks";
                                    record.RecordingAvailableUrl = "/record/callbacks";
                                    record.MaxDuration = 60;
                                    record.TerminatingDigits = "#";

                                    bxmlResponse.Add(record);
                                }
                                break;
                            case "gather":
                                string digits = callback.digits;
                                if (digits.Equals("1"))
                                {

                                    var speakSentence = new SpeakSentence();
                                    speakSentence.Sentence = "Please, leave a voicemail after the beep and press # when done, beep.";
                                    speakSentence.Voice = "julie";

                                    var redirect = new Redirect();
                                    redirect.RedirectUrl = "/record/start";

                                    bxmlResponse.Add(speakSentence);
                                    bxmlResponse.Add(redirect);

                                }
                                else
                                {
                                    var speakSentence = new SpeakSentence();
                                    speakSentence.Sentence = "Thank you, a text message of the recording has been sent to ";
                                    speakSentence.Voice = "julie";

                                    bxmlResponse.Add(speakSentence);

                                    sendRecordingAsMMSMessage((string)callback.tag, "+19192347322", (string)callback.from);

                                    //TODO send MMS Msg
                                    Console.WriteLine("Call Finished");
                                }
                                break;
                            default:
                                bxmlResponse.Add(new Hangup()); //All others hangup.
                                break;
                        }
                        break;
                    default:
                        bxmlResponse.Add(new Hangup()); //We will not be makeing outboud calls.
                        break;
                }

                return bxmlResponse.ToBXML();
            });

            post("/record/callbacks", (request, response) => {

                string json = ControllerHelpers.getBody(request);
                dynamic callback = APIHelper.JsonDeserialize<dynamic>(json);
                Response bxmlResponse = new Response();

                switch ((string)callback.direction)
                {
                    case "inbound":
                        switch ((string)callback.eventType)
                        {
                            case "recordComplete":
                                {
                                    Console.WriteLine("Recod Complete");
                                    var pause = new Pause();
                                    pause.Duration = 60;

                                    var speakSentence = new SpeakSentence();
                                    speakSentence.Sentence = "Time is up";

                                    bxmlResponse.Add(pause);
                                    bxmlResponse.Add(speakSentence);
                                }
                                break;
                            case "recordingAvailable":
                                Console.WriteLine("Recod Available:  ");
                                ApiModifyCallRequest apiModifyCallRequest = new ApiModifyCallRequest();

                                apiModifyCallRequest.RedirectUrl = voiceServer + "/record/check";
                                apiModifyCallRequest.Tag = callback.mediaUrl;
                                voiceController.ModifyCall(voiceAccountId, (string)callback.callId, apiModifyCallRequest);
                                break;
                            default:
                                break;
                        }
                        break;
                    default:
                        bxmlResponse.Add(new Hangup()); //We will not be makeing outboud calls.
                        break;
                }



                return bxmlResponse.ToBXML();
            });

            post("/call/status", (request, response) =>
            {
                string json = ControllerHelpers.getBody(request);
                Console.WriteLine(json);
                return "";
            });

            post("/msg", (request, response) =>
            {
                string json = ControllerHelpers.getBody(request);
                dynamic callback = APIHelper.JsonDeserialize<dynamic>(json);

                Console.WriteLine("Message Callback: " + (string)callback[0].type);

                if("message-failed".Equals( (string)callback[0].type) ) {
                    Console.WriteLine("errorCode: " + (string)callback[0].errorCode);
                    Console.WriteLine("errorCode: " + (string)callback[0].description);
                    Console.WriteLine("message ID: " + (string)callback[0].message.id);
                }

                return "";
            });

            while (isRunning())
            {
                Thread.Sleep(1000);
            }

        }

        static void sendRecordingAsMMSMessage(string mediaUrl, string from, string to)
        {

            string newMediaUrl = uploadMediaToMessage(mediaUrl);

            Console.WriteLine(newMediaUrl);

            MessageRequest messageRequest = new MessageRequest();
            messageRequest.Media = new List<string>() { newMediaUrl };
            messageRequest.Text = "This is your recording";
            messageRequest.To = new List<string>() { to };
            messageRequest.From = from;
            messageRequest.ApplicationId = msgApplicationId;

            Console.WriteLine("To:  " + messageRequest.To[0]);
            Console.WriteLine("From:  " + messageRequest.From);

            msgController.CreateMessage(msgAccountId, messageRequest);
        }

        static string uploadMediaToMessage(string source)
        {

            Uri uri = new Uri(source);

            string recordingId = uri.Segments[uri.Segments.Length - 2].Replace("/","");
            string callId = uri.Segments[uri.Segments.Length - 4].Replace("/", "");

            var mediaReply = voiceController.GetStreamRecordingMedia(voiceAccountId, callId, recordingId);

            FileStreamInfo fsi = new FileStreamInfo( mediaReply.Data, recordingId + ".wav", "audio/wav");

            /**
            string test = null;
            using( StreamReader sr = new StreamReader(mediaReply.Data))
            {
                test = sr.ReadToEnd();
            }

            */

            msgController.UploadMedia(msgAccountId, recordingId , mediaReply.Data.Length, fsi, "application/octet-stream", "no-cache");
      
            return "https://messaging.bandwidth.com/api/v2/users/"+ msgAccountId +"/media/" + recordingId;
            
        }

        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }
    }
}
