using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using DocToSpeech.Shared;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace DocToSpeech.Functions
{
    public static class DocToSpeech
    {
		[FunctionName("doctospeech")]
		public static async Task<HttpResponseMessage> HttpStart(
			[HttpTrigger(AuthorizationLevel.Anonymous, "post")]ConvertRequest request, 
			[OrchestrationClient]DurableOrchestrationClient starter,
			ILogger log)
		{

			var instanceId = await starter.StartNewAsync(nameof(RunOrchestrator), request);
			log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

			return new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(instanceId) };
		}
		
		[FunctionName(nameof(RunOrchestrator))]
        public static async Task<ConvertResponse> RunOrchestrator([OrchestrationTrigger] DurableOrchestrationContext context, ExecutionContext exContext,
			[SignalR(HubName = "status")]IAsyncCollector<SignalRMessage> signalRMessages)
        {
			var request = context.GetInput<ConvertRequest>();
			var config = exContext.GetConfig();

			var response = new ConvertResponse
			{
				RequestId = context.InstanceId,
				Request = request,
			};

			response = await context.CallActivityAsync<ConvertResponse>(nameof(ExtractTextFromImageComputerVision), response);
			response = await context.CallActivityAsync<ConvertResponse>(nameof(ConvertTextToSpeechSsml), response);

			if(response.SpeechAudioBlobUrl != null)
				response.SpeechAudioBlobUrl += config["storage:sas"];

			if (response.TranscriptBlobUrl != null)
				response.TranscriptBlobUrl += config["storage:sas"];

			await signalRMessages.AddAsync(new SignalRMessage
			{
				Target = "newSpeechFile",
				Arguments = new[] { response }
			});

			return response;
        }

		[FunctionName(nameof(ExtractTextFromImageComputerVision))]
		public static async Task<ConvertResponse> ExtractTextFromImageComputerVision([ActivityTrigger] ConvertResponse response, ILogger log, ExecutionContext context)
		{
			var config = context.GetConfig();
			var client = new TextExtractionClient(config);
			
			var newResponse = await client.ExtractTextBatchRead(response);
			return newResponse;
		}

		[FunctionName(nameof(ConvertTextToSpeechSsml))]
		public static async Task<ConvertResponse> ConvertTextToSpeechSsml([ActivityTrigger] ConvertResponse response, ILogger log, ExecutionContext context)
		{
			if(response.ErrorMessage != null)
				return response;
	
			var config = context.GetConfig();
			var client = new TextToSpeechClient(config);

			var newResponse = await client.ConvertTextToSpeechSsml(response);
			return newResponse;
		}
    }
}