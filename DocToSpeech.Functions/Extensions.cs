using Microsoft.Extensions.Configuration;
using Microsoft.Azure.WebJobs;

namespace DocToSpeech.Functions
{
	public static class Extensions
	{
		public static IConfiguration GetConfig(this ExecutionContext context)
		{
			return new ConfigurationBuilder()
				.SetBasePath(context.FunctionAppDirectory)
				.AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
				.AddEnvironmentVariables()
				.Build();

		}
	}
}