

using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Configuration;

namespace DocToSpeech.Functions
{
	public class BlobStorageClient
	{
		IConfiguration _config;
		public BlobStorageClient(IConfiguration config)
		{
			_config = config;
		}

		public string GetBlobUrl(string container, string blobName)
		{
			var sas = _config["storage:sas"];
			var storageCredentials = new StorageCredentials(_config["storage:resourceName"], _config["storage:key"]);
			var storageAccount = new CloudStorageAccount(storageCredentials, true);
			var blobClient = storageAccount.CreateCloudBlobClient();
			var containerClient = blobClient.GetContainerReference(container);

			var blob = containerClient.GetBlobReference(blobName);
			var url = blob.Uri.AbsoluteUri + sas;
			return url;
		}

		public CloudBlockBlob GetBlobBlock(string container, string blobName)
		{
			var storageCredentials = new StorageCredentials(_config["storage:resourceName"], _config["storage:key"]);
			var storageAccount = new CloudStorageAccount(storageCredentials, true);
			var blobClient = storageAccount.CreateCloudBlobClient();
			var containerClient = blobClient.GetContainerReference(container);

			var blob = containerClient.GetBlockBlobReference(blobName);
			return blob;
		}
	}
}