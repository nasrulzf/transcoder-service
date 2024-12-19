using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Transcoder.Web.Models;

namespace Transcoder.Web.Controllers
{
    [Route("[controller]")]
    public class VideoController : Controller
    {
        private readonly ILogger<VideoController> _logger;
        private readonly IFileProvider _fileProvider;

        public VideoController(ILogger<VideoController> logger, IFileProvider fileProvider)
        {
            _logger = logger;
            _fileProvider = fileProvider;
        }

        [HttpGet("Index")]
        public IActionResult Index()
        {
            var directories = Directory.GetDirectories("/data");
            var folders = directories.Select(a => Path.GetFileName(a)).ToList();
            return View(folders);
        }

        [HttpGet("Player")]
        public IActionResult Detail(string file)
        {
            _logger.LogInformation("Get request with File = {0}", file);
            string pathFile = Path.Combine("/data", file);
            if (!Directory.Exists(pathFile))
            {
                _logger.LogError("File directory not found. File = {0}", file);
                throw new FileNotFoundException("File not found");
            }
            _logger.LogInformation("Directory found, request with File = {0}", file);
            return View("Detail", file);
        }

        [HttpHead("File"), HttpGet("File")]
        public IActionResult FileAction(string name)
        {
            _logger.LogInformation("Get request with File = {0}", name);

            var fileInfo = _fileProvider.GetFileInfo(name);
            if (fileInfo.Exists)
            {
                _logger.LogInformation("File found, File = {0}", name);

                return File(fileInfo.CreateReadStream(), "application/octet-stream", fileInfo.Name);
            }
            _logger.LogInformation("File not found, File = {0}", name);
            return NotFound();

        }
    }
}