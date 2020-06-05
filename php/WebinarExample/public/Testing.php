<?php


use BandwidthLib\Messaging\Models\BandwidthCallbackMessage;
use BandwidthLib\Messaging\Models\BandwidthMessage;
use Psr\Http\Message\ResponseInterface as Response;
use Psr\Http\Message\ServerRequestInterface as Request;
use Slim\Factory\AppFactory;
require __DIR__ . '/../vendor/autoload.php';
$BANDWIDTH_ACCOUNT_ID           = "9900778";
$BANDWIDTH_API_USER             = "USER";
$BANDWIDTH_API_PASSWORD         = "PASSWORD";
$BANDWIDTH_VOICE_APPLICATION_ID = "04e88489-df02-4e34-a0ee-27a91849555f";
$config = new BandwidthLib\Configuration(
    array(
        'voiceBasicAuthUserName' => $BANDWIDTH_API_USER,
        'voiceBasicAuthPassword' => $BANDWIDTH_API_PASSWORD,
    )
);
$client = new BandwidthLib\BandwidthClient($config);
$transferNumber = '+19198021884';
// Instantiate App
$app = AppFactory::create();
// Add error middleware
$app->addErrorMiddleware(true, true, true);
$app->post('/Callbacks/Voice/Inbound', function (Request $request, Response $response) {
    $speakSentence = new BandwidthLib\Voice\Bxml\SpeakSentence('Hello, We are going to transfer this call');
    $speakSentence->voice("kate");
    $bxmlResponse = new BandwidthLib\Voice\Bxml\Response();
    $number = new BandwidthLib\Voice\Bxml\PhoneNumber('+19198021884');
    $number->transferAnswerUrl('/Callbacks/Voice/TransferAnswer');
    $transfer = new BandwidthLib\Voice\Bxml\Transfer();
    $transfer->transferCallerId("+19198675309");
    $transfer->transferCompleteUrl('/Callbacks/Voice/TransferComplete');
    $transfer->callTimeout(1);
    $transfer->phoneNumbers(array($number));
    $bxmlResponse->addVerb($speakSentence);
    $bxmlResponse->addVerb($transfer);
    $bxml = $bxmlResponse->toBxml();
    $response = $response->withStatus(200)->withHeader('Content-Type', 'application/xml');
    $response->getBody()->write($bxml);
    return $response;
});
$app->post('/Callbacks/Voice/TransferAnswer', function (Request $request, Response $response) {
    $speakSentence = new BandwidthLib\Voice\Bxml\SpeakSentence('Hello, this is the transfer answer URL');
    $speakSentence->voice("kate");
    $bxmlResponse = new BandwidthLib\Voice\Bxml\Response();
    $bxmlResponse->addVerb($speakSentence);
    $bxml = $bxmlResponse->toBxml();
    $response = $response->withStatus(200)->withHeader('Content-Type', 'application/xml');
    $response->getBody()->write($bxml);
    return $response;
});
$app->post('/Callbacks/Voice/TransferComplete', function (Request $request, Response $response) {
    $speakSentence = new BandwidthLib\Voice\Bxml\SpeakSentence('Hello, this is the transfer complete URL');
    $speakSentence->voice("kate");
    $bxmlResponse = new BandwidthLib\Voice\Bxml\Response();
    $bxmlResponse->addVerb($speakSentence);
    $bxml = $bxmlResponse->toBxml();
    $response = $response->withStatus(200)->withHeader('Content-Type', 'application/xml');
    $response->getBody()->write($bxml);
    return $response;
});
$app->run();