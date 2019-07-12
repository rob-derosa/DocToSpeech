using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;

namespace DocToSpeech.Functions
{
	public static class NegotiateFunction
	{
		[FunctionName("negotiate")]
		public static SignalRConnectionInfo Negotiate([HttpTrigger(AuthorizationLevel.Anonymous, "post")]HttpRequest req, [SignalRConnectionInfo(HubName = "status")] SignalRConnectionInfo connectionInfo)
		{
			// connectionInfo contains an access key token with a name identifier claim set to the authenticated user
			return connectionInfo;
		}
	}
}