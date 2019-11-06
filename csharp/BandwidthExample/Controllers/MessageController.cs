using System.Collections.Generic;
using System;
using System.IO;
using Helpers;
using Bandwidth.Standard.Messaging.Controllers;
using Bandwidth.Standard.Messaging.Models;
using Bandwidth.Standard.Utilities;
using Bandwidth.Standard;

using static Eagle.Server;

using static System.Console;
using System.Text;

namespace Controllers {
	/**
	 * Controller to handle the Bandwidth message API
	  */
	public class MessageController {

		static readonly Configuration config = new Configuration.Builder()
			.WithMessagingBasicAuthPassword( Environment.GetEnvironmentVariable("MSG_API_SECRET") )
            .WithMessagingBasicAuthUserName (Environment.GetEnvironmentVariable("MSG_API_USER"))
            .WithEnvironment(Configuration.Environments.PRODUCTION)
            .Build();

        private static APIController msgClient = new BandwidthClient(config).Messaging.Client;

    	private  static string msgUserId ="";
		private static readonly string applicationId =  "";

		/**
		* Uploads a media file from the disk to the Bandwidth network
		* @param fileURL
		* @param contentType
		* @param mediaId
     	*/
		public static void uploadMedia(string fileURL, string contentType, string mediaId) {

       		if(!File.Exists(fileURL)) return;

			FileInfo fileInfo = new FileInfo(fileURL);
            var buffer = File.ReadAllBytes(fileURL);

            try {
            	msgClient.UploadMedia(msgUserId, mediaId, fileInfo.Length, Encoding.UTF8.GetString(buffer, 0, buffer.Length) ,contentType, "no-cache" );
        	} catch (APIException e) {
            	WriteLine(e.Message);
        	} catch (IOException e) {
            	WriteLine(e.Message);
        	}
    	}

		/**
		* Downloads media from the Bandwidth network to local
		*/
    	public static void downloadMedia(){

    	}


		/**
		* List the media in the user's account
		* @return
     	*/	
	 	public static List<Media> listMedia() {

			List<Media> list = null;
			try {
				list = msgClient.ListMedia(msgUserId,"").Data;
			} catch (APIException e) {
            	WriteLine(e.Message);
        	} catch (IOException e) {
            	WriteLine(e.Message);
        	}

			return list;

		}

		/**
		* Starts a post http listner for an incoming message.
		* <br/>
		* If the incoming message text is "call me" it will initate a voice call with the text sender
		* <br/>
		* If the incoming message contains media it will send the media back to the sender
		* <br/>
		* If the incoming message is not "call me" and contains no media it will reply with a sentence to the sender.
		*/
		public static void listenReplyToMessage() {

			post("/msg/incoming", (request, response) => {

				string json = ControllerHelpers.getBody(request);

				BandwidthCallbackMessage[] callbackMessages = APIHelper.JsonDeserialize<BandwidthCallbackMessage[]>(json);

				if(callbackMessages == null || callbackMessages.Length == 0  ){
					//Incorrect format return
					return "";
				}

				if("message-delivered".Equals(callbackMessages[0].Type) || "message-failed".Equals(callbackMessages[0].Type)){
					//Message delivery notice or message failed notice.  Return 200 to Bandwidth.
					WriteLine(callbackMessages[0].Type);
					return "";
				}

				// Incoming message to application # callbackMessages[0].getType() equals "message-received"

				//For inbound messages, the From number is the number that sent the text
				string from = callbackMessages[0].Message.From;

				//Set outbound message to the inbound number. i.e. the message sender to reply
				List<string> sendToNums = new List<string>();
				sendToNums.Add(from);

				//For inbound messages the "To" number list is the target numbers 
				//The sender wanted to message
				string to = callbackMessages[0].Message.To[0];

				//Create a MessageRequest that replies to the inbound message
				//This can be done by flipint the "To" with the "From"
				MessageRequest msgRequest = new MessageRequest();
				msgRequest.ApplicationId = applicationId;
				msgRequest.From = to;//The first number the sender wanted to message
				msgRequest.To = sendToNums;

				string incomingText = callbackMessages[0].Message.Text;

				List<string> incomingMedia = callbackMessages[0].Message.Media;

				if("call me".Equals(incomingText.Trim().ToLower())){
					//Flip the inbound "From" and "To" to make a call to the inbound sender.
					VoiceController.makeOutboudCall(from, to);
					return "";
				} else if( incomingMedia == null || incomingMedia.Count == 0 ) {
					msgRequest.Text = "The quick brown fox jumps over a lazy dog.";
				} else {

					//Download the incoming media to temp area on disk

					//Upload the media from the disk to bandwidth with diffrent name

					//Send the new media back to the texter

					msgRequest.Media = incomingMedia;
				}

				msgClient.CreateMessage(msgUserId, msgRequest);

				return "";
			});
		}




	}
}