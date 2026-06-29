using Microsoft.AspNetCore.Mvc;
using OmrSheet.Services;
using OmrSheet.Models;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace OmrSheet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OMRController : ControllerBase
    {
        private readonly OMRService _omrService;

        public OMRController()
        {
            _omrService = new OMRService();
        }

        // ── GET /api/OMR/Templates ───────────────────────────────────────
        [HttpGet("templates")]
        public IActionResult GetTemplates()
        {
            return Ok(TemplateProvider.GetTemplates());
        }

        // ── POST /api/OMR/Upload ─────────────────────────────────────────
        [HttpPost("upload")]
        public async Task<IActionResult> Upload(IFormFile file, [FromForm] string templateId = "Template1")
        {
            var templateConfig = TemplateProvider.GetTemplate(templateId);

            if (file == null || file.Length == 0)
            { 
                return BadRequest(new { Message = "Please select a file." }); 
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            { 
                return BadRequest(new { Message = "Invalid file type. Please upload a JPG, PNG, or BMP image." }); 
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "Uploads");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);
            foreach (var oldFile in Directory.GetFiles(uploadsFolder))
            {
                try { System.IO.File.Delete(oldFile); } catch { }
            }

            var uniqueFileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            Dictionary<int, string> studentAnswers;
            try 
            { 
                studentAnswers = _omrService.ProcessOMR(filePath, templateConfig); 
            }
            catch (Exception ex)
            {
                Console.WriteLine("OMR error: " + ex.Message);
                return StatusCode(500, new { Message = "Error processing OMR sheet. Please upload a clear image." });
            }

            return Ok(new {
                StudentAnswers = studentAnswers
            });
        }
    }
}