using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DocToSpeech.Shared;
using Microsoft.Extensions.Configuration;

namespace DocToSpeech.Functions
{
	public class TextToSpeechClient
	{
		IConfiguration _config;
		public TextToSpeechClient(IConfiguration config)
		{
			_config = config;
		}

		public async Task<ConvertResponse> ConvertTextToSpeechSsml(ConvertResponse response)
		{
			var ext = new FileInfo(response.Request.BlobName).Extension;
			var waveName = response.Request.BlobName.Replace(ext, ".mp3");

			try
			{
				string accessToken = null;
				using (var client = new HttpClient())
				{
					client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _config["speech:key"]);
					var tokenFetchUri = $"https://{_config["speech:region"]}.api.cognitive.microsoft.com/sts/v1.0/issueToken";
					var uriBuilder = new UriBuilder(tokenFetchUri);

					var result = await client.PostAsync(uriBuilder.Uri.AbsoluteUri, null).ConfigureAwait(false);
					accessToken = await result.Content.ReadAsStringAsync().ConfigureAwait(false);
				}

				var r = response.Request;
				var host = $"https://{_config["speech:region"]}.tts.speech.microsoft.com/cognitiveservices/v1";
				var prosody = $"<prosody rate='{r.Speed}.00%' pitch='{r.Pitch}' volume='{r.Volume}'>";

				response.Transcript = response.Transcript.Replace("&", "and");

				var transcript = response.Transcript;
				var maxChars = 10000;
				if(transcript.Length > maxChars)
				{
					// var intro = "This transcript is too long and has been shortened automatically." + Environment.NewLine;
					//transcript = intro + transcript.Substring(0, maxChars - intro.Length);
					transcript = transcript.Substring(0, maxChars);
				}

				string body = $@"<speak version='1.0' xmlns='https://www.w3.org/2001/10/synthesis' xml:lang='en-US'>
					<voice name='Microsoft Server Speech Text to Speech Voice (en-US, {r.VoiceToUse})'>{prosody}" +
						transcript + "</prosody></voice></speak>";

				Console.WriteLine();
				Console.WriteLine(body);

				using (var client = new HttpClient())
				{
					using (var request = new HttpRequestMessage())
					{
						request.Method = HttpMethod.Post;
						request.RequestUri = new Uri(host);
						request.Content = new StringContent(body, Encoding.UTF8, "application/ssml+xml");
						request.Headers.Add("Authorization", "Bearer " + accessToken);
						request.Headers.Add("Connection", "Keep-Alive");
						request.Headers.Add("User-Agent", _config["speech:resourceName"]);
						request.Headers.Add("X-Microsoft-OutputFormat", "audio-16khz-64kbitrate-mono-mp3");

						Console.WriteLine("Calling the TTS service. Please wait... \n");
						using (var speechResponse = await client.SendAsync(request).ConfigureAwait(false))
						{
							if((int)speechResponse.StatusCode == 503)
							{
								response.ErrorMessage = "Transcript too large to convert to speech.";
								return response;
							}

							speechResponse.EnsureSuccessStatusCode();
							using (var dataStream = await speechResponse.Content.ReadAsStreamAsync().ConfigureAwait(false))
							{
								var fileName = Path.GetTempPath() + "/temp.mp3";

								Console.WriteLine("Your speech file is being written to temp file...");
								using (var fileStream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.Write))
								{
									await dataStream.CopyToAsync(fileStream).ConfigureAwait(false);
									fileStream.Close();
								}

								var blobClient = new BlobStorageClient(_config);
								var audioBlob = blobClient.GetBlobBlock("speechresults", waveName);
								await audioBlob.UploadFromFileAsync(fileName);

								File.Delete(fileName);

								response.SpeechAudioBlobUrl = audioBlob.Uri.AbsoluteUri;
								response.IsSuccessful = true;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.ToString());
				Console.WriteLine(ex.Message);
				response.ErrorMessage = ex.ToString();
			}

			return response;
		}
	}
}