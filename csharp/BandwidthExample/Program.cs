using System;
using Controllers;

using static Eagle.Server;


namespace BandwidthExample
{
    class Program
    {

        static void Main(string[] args)
        {

			port("8080");

			MessageController.listMedia();

			MessageController.listenReplyToMessage();

        	VoiceController.letsPlayAGame();
        	VoiceController.callMeMessage();
        	VoiceController.gatherAndTransfer();
		
            Console.Read();

        }

		
      
    }
}
