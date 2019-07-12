namespace DocToSpeech.Shared
{
	public class ConvertResponse
	{
		public string RequestId { get; set; }
		public ConvertRequest Request { get; set; }
		public string TranscriptBlobUrl { get; set; }
		public string Transcript { get; set; }
		public string SpeechAudioBlobUrl { get; set; }
		public string ErrorMessage { get; set; }
		public bool IsSuccessful { get; set; }
	}
}