using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using DocToSpeech.Web.Models;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage;
using System.Net.Http;
using DocToSpeech.Shared;
using Newtonsoft.Json;
using System.Text;

namespace DocToSpeech.Web.Controllers
{

    public class HomeController : Controller
    {
		[BindProperty]
		public string Voice { get; set; }

		[BindProperty]
		public string Pitch { get; set; }

		[BindProperty]
		public int Speed { get; set; }

		[BindProperty]
		public int Volume { get; set; }

		IConfiguration _config;
		public HomeController(IConfiguration config)
		{
			_config = config;
		}
		
        public IActionResult Index()
        {
			ViewBag.ApiEndpoint = _config["api:endpoint"];
			
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

		[HttpPost("UploadFiles")]
		[DisableRequestSizeLimit]

		public async Task<IActionResult> Post(List<IFormFile> uploadFiles)
		{
			var uploadSuccess = false;
			string instanceId = null;

			if (!uploadFiles.Any(f => f.FileName.ToLower().EndsWith(".png")
				|| f.FileName.ToLower().EndsWith(".jpg")
				|| f.FileName.ToLower().EndsWith(".pdf")))
			{
				TempData["Error"] = "Please select a valid file type (png, jpg, pdf).";
				return RedirectToAction("Index");
			}

			foreach (var formFile in uploadFiles)
			{
				if (formFile.Length <= 0)
				{
					continue;
				}

				using (var stream = formFile.OpenReadStream())
				{
					var storageCredentials = new StorageCredentials("doctospeechstorage", _config["storage:key"]);
					var storageAccount = new CloudStorageAccount(storageCredentials, true);
					var blobClient = storageAccount.CreateCloudBlobClient();
					var containerClient = blobClient.GetContainerReference("uploads");

					var blobName = $"{Path.GetFileName(formFile.FileName)}_{Voice}";
					var blob = containerClient.GetBlockBlobReference(blobName);
					await blob.UploadFromStreamAsync(stream);

					TempData["uploadedUri"] = blob.Uri.AbsoluteUri;
					uploadSuccess = true;

					var client = new HttpClient();

					var request = new ConvertRequest
					{
						BlobName = blob.Name,
						UploadBlobUrl = blob.Uri.AbsoluteUri,
						VoiceToUse = Voice,
						Pitch = Pitch,
						Volume = Volume,
						Speed = Speed
					};
					
					var url = $"{_config["api:endpoint"]}/doctospeech";
					var json = JsonConvert.SerializeObject(request);
					var response = await client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
					instanceId = await response.Content.ReadAsStringAsync();
				}
			}

			if (uploadSuccess)
				return RedirectToAction("Index", new { requestId = instanceId });
			else
				return View("UploadError");
		}
    }
}
