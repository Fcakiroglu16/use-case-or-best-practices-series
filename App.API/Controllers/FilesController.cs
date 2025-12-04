using Microsoft.AspNetCore.Mvc;

namespace App.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]

    public class FilesController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FilesController> _logger;

        public FilesController(IWebHostEnvironment environment, ILogger<FilesController> logger)
        {
            _environment = environment;
            _logger = logger;
        }


        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new { message = "FilesController is up and running." });
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file uploaded" });
            }

            string imagesPath = Path.Combine(Environment.CurrentDirectory, "wwwroot", "images");



            string fileName = $"{Guid.NewGuid()}_{file.FileName}";
            string filePath = Path.Combine(imagesPath, fileName);

            FileStream stream = new FileStream(filePath, FileMode.Create);

            await file.CopyToAsync(stream);


            _logger.LogInformation("File uploaded: {FileName}, Size: {FileSize} bytes", fileName, file.Length);

            return Ok(new
            {
                fileName = fileName,
                path = $"/images/{fileName}",
                size = file.Length
            });
        }
    }
}
