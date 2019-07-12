using System;

namespace DocToSpeech.Shared
{
public class ConvertRequest
	{
		public string BlobName { get; set; }
		public string UploadBlobUrl { get; set; }
		public string VoiceToUse { get; set; }
		public string TextExtractionType { get; set; }
		public string Pitch { get; set; }
		public int Speed { get; set; }
		public int Volume { get; set; }
	}

	public enum TextExtractionType
	{
		PrintedText,
		HandwrittenText,
		BatchRead,
	}
}
