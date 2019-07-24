using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocToSpeech.Shared;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Configuration;

namespace DocToSpeech.Functions
{
	public class TextExtractionClient
	{
		IConfiguration _config;
		public TextExtractionClient(IConfiguration config)
		{
			_config = config;
		}

		public async Task<ConvertResponse> ExtractTextComputerVision(ConvertResponse response)
		{
			try
			{
				var key = _config["computerVision:key"];
				var endpoint = _config["computerVision:endpoint"];
				ComputerVisionClient computerVision = new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
				{
					Endpoint = endpoint
				};

				var analysis = await computerVision.RecognizePrintedTextAsync(true, response.Request.UploadBlobUrl + _config["storage:sas"]);

				var text = new StringBuilder();
				foreach (var region in analysis.Regions)
				{
					foreach (var line in region.Lines)
					{
						foreach (var word in line.Words)
						{
							text.Append(word.Text + " ");
						}
						text.AppendLine();
					}
				}

				var transcriptBlobName = Path.GetFileNameWithoutExtension(response.Request.BlobName) + ".txt";

				var blobClient = new BlobStorageClient(_config);
				var textBlob = blobClient.GetBlobBlock("transcripts", transcriptBlobName);

				response.TranscriptBlobUrl = textBlob.Uri.AbsoluteUri;
				response.Transcript = text.ToString().Trim();

				await textBlob.UploadTextAsync(response.Transcript);
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex.ToString());
				Console.WriteLine(ex.Message);
				response.ErrorMessage = ex.ToString();
			}

			return response;
		}

		public async Task<ConvertResponse> ExtractTextBatchRead(ConvertResponse response)
		{
			try
			{
				var key = _config["computerVision:key"];
				var endpoint = _config["computerVision:endpoint"];
				ComputerVisionClient computerVision = new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
				{
					Endpoint = endpoint
				};

				var operationInfo = await computerVision.BatchReadFileAsync(response.Request.UploadBlobUrl + _config["storage:sas"]);

				var result = new ReadOperationResult();
				var operationId = operationInfo.OperationLocation.Split('/').Last();

				while (result.Status != TextOperationStatusCodes.Failed && result.Status != TextOperationStatusCodes.Succeeded)
				{
					await Task.Delay(500);
					result = await computerVision.GetReadOperationResultAsync(operationId);
				}

				if (result.Status == TextOperationStatusCodes.Failed)
				{
					response.ErrorMessage = $"Text translation failed.";
					return response;
				}

				var text = new StringBuilder();
				foreach (var page in result.RecognitionResults)
				{
					Line lastLine = null;
					foreach (var line in page.Lines)
					{
						// if (lastLine?.Words.Count >= 4)
						// {
						// 	text.Append($" {line.Text}");
						// }
						// else
						// {
							text.Append(Environment.NewLine + line.Text);
						// }

						lastLine = line;
					}
				}

				Console.WriteLine();
				Console.WriteLine(text.ToString());

				var transcriptBlobName = Path.GetFileNameWithoutExtension(response.Request.BlobName) + ".txt";

				var blobClient = new BlobStorageClient(_config);
				var textBlob = blobClient.GetBlobBlock("transcripts", transcriptBlobName);

				response.TranscriptBlobUrl = textBlob.Uri.AbsoluteUri;
				response.Transcript = text.ToString().Trim();

				await textBlob.UploadTextAsync(response.Transcript);
			}
			catch(Exception ex)
			{
				Console.WriteLine(ex.ToString());
				Console.WriteLine(ex.Message);
				response.ErrorMessage = ex.ToString();
			}

			return response;
		}
	}
}