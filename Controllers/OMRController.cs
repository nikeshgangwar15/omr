using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OmrSheet.Data;
using OmrSheet.Models;
using OmrSheet.Services;
using System.Text.Json;

namespace OmrSheet.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OMRController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly OMRService _omrService;

        // Fallback only — used if teacher hasn't set a key yet
        private static readonly Dictionary<int, string> DefaultAnswerKey =
            new Dictionary<int, string>()
        {
            { 1,"A"},{ 2,"A"},{ 3,"A"},{ 4,"A"},{ 5,"A"},
            { 6,"A"},{ 7,"A"},{ 8,"A"},{ 9,"A"},{10,"A"},
            {11,"A"},{12,"A"},{13,"A"},{14,"A"},{15,"A"},
            {16,"A"},{17,"A"},{18,"A"},{19,"A"},{20,"A"}
        };

        public OMRController(AppDbContext context)
        {
            _context = context;
            _omrService = new OMRService();
        }

        // ── Load answer key from DB ──────────────────────────────────────
        private async Task<ExamAnswerKey> GetAnswerKeyRecordAsync(string templateId)
        {
            var keyRecord = await _context.AnswerKeys
                .Where(k => k.TemplateId == templateId)
                .OrderByDescending(k => k.UpdatedAt)
                .FirstOrDefaultAsync();

            if (keyRecord == null)
            {
                keyRecord = new ExamAnswerKey
                {
                    TemplateId = templateId,
                    AnswersJson = JsonSerializer.Serialize(DefaultAnswerKey),
                    CorrectMark = 4,
                    IncorrectMark = -1,
                    UnattemptedMark = 0
                };
            }
            else if (string.IsNullOrWhiteSpace(keyRecord.AnswersJson))
            {
                keyRecord.AnswersJson = JsonSerializer.Serialize(DefaultAnswerKey);
            }

            return keyRecord;
        }

        private Dictionary<int, string> ParseAnswerKey(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return DefaultAnswerKey;
            return JsonSerializer.Deserialize<Dictionary<int, string>>(json) ?? DefaultAnswerKey;
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
            foreach (var oldFile in Directory.GetFiles(uploadsFolder)) System.IO.File.Delete(oldFile);

            var uniqueFileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var answerKeyRecord = await GetAnswerKeyRecordAsync(templateId);
            var answerKey = ParseAnswerKey(answerKeyRecord.AnswersJson);

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

            int marks = _omrService.CalculateMarks(answerKey, studentAnswers, answerKeyRecord.CorrectMark, answerKeyRecord.IncorrectMark, answerKeyRecord.UnattemptedMark);
            int attempted = studentAnswers.Count(a => !string.IsNullOrEmpty(a.Value));
            int unattempted = studentAnswers.Count(a => string.IsNullOrEmpty(a.Value));
            int correct = studentAnswers.Count(a => answerKey.ContainsKey(a.Key) && a.Value == answerKey[a.Key]);
            int wrong = attempted - correct;

            string answersJson = JsonSerializer.Serialize(studentAnswers);

            try
            {
                var omrSheet = new OMRSheet
                {
                    StudentId = "ST" + new Random().Next(1000, 9999),
                    OMRId = Guid.NewGuid().ToString(),
                    MarksObtained = marks,
                    FilePath = filePath,
                    UploadedAt = DateTime.Now,
                    AnswersJson = answersJson,
                    TemplateId = templateId,
                    IsArchived = false
                };

                _context.OMRSheets.Add(omrSheet);
                await _context.SaveChangesAsync();

                var deserializedAnswers = JsonSerializer.Deserialize<Dictionary<int, string>>(answersJson)
                                          ?? new Dictionary<int, string>();

                return Ok(new {
                    StudentId = omrSheet.StudentId,
                    OMRId = omrSheet.OMRId,
                    Marks = marks,
                    Attempted = attempted,
                    Unattempted = unattempted,
                    Correct = correct,
                    Wrong = wrong,
                    Total = answerKey.Count,
                    MaxMarks = answerKey.Count * answerKeyRecord.CorrectMark,
                    StudentAnswers = deserializedAnswers,
                    AnswerKey = answerKey
                });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                return StatusCode(500, new { Message = "Error saving to database: " + inner });
            }
        }

        // ── GET /api/OMR/AnswerKey ───────────────────────────────────────
        [HttpGet("answerkey")]
        public async Task<IActionResult> GetAnswerKey([FromQuery] string templateId = "Template1")
        {
            var keyRecord = await GetAnswerKeyRecordAsync(templateId);
            var currentKey = ParseAnswerKey(keyRecord.AnswersJson);

            return Ok(new {
                KeySet = keyRecord.Id != 0,
                LastUpdated = keyRecord.Id != 0 ? keyRecord.UpdatedAt.ToString("MMM dd, yyyy HH:mm") : null,
                AnswerKey = currentKey,
                SelectedTemplateId = templateId,
                SelectedTemplate = TemplateProvider.GetTemplate(templateId),
                CorrectMark = keyRecord.CorrectMark,
                IncorrectMark = keyRecord.IncorrectMark,
                UnattemptedMark = keyRecord.UnattemptedMark
            });
        }

        // ── POST /api/OMR/AnswerKey ──────────────────────────────────────
        public class AnswerKeyUpdateRequest
        {
            public Dictionary<string, string> Answers { get; set; } = new Dictionary<string, string>();
            public int CorrectMark { get; set; } = 4;
            public int IncorrectMark { get; set; } = -1;
            public int UnattemptedMark { get; set; } = 0;
        }

        [HttpPost("answerkey")]
        public async Task<IActionResult> UpdateAnswerKey([FromBody] AnswerKeyUpdateRequest request, [FromQuery] string templateId = "Template1")
        {
            var templateConfig = TemplateProvider.GetTemplate(templateId);

            var newKey = new Dictionary<int, string>();
            for (int q = 1; q <= templateConfig.TotalQuestions; q++)
            {
                string val = "A";
                if (request.Answers != null && request.Answers.ContainsKey($"q{q}"))
                {
                    val = request.Answers[$"q{q}"].Trim().ToUpper();
                    if (string.IsNullOrEmpty(val)) val = "A";
                }
                newKey[q] = val;
            }

            string json = JsonSerializer.Serialize(newKey);

            var existing = await _context.AnswerKeys.FirstOrDefaultAsync(k => k.TemplateId == templateId);
            if (existing != null)
            {
                existing.AnswersJson = json;
                existing.CorrectMark = request.CorrectMark;
                existing.IncorrectMark = request.IncorrectMark;
                existing.UnattemptedMark = request.UnattemptedMark;
                existing.UpdatedAt = DateTime.Now;
                _context.AnswerKeys.Update(existing);
            }
            else
            {
                _context.AnswerKeys.Add(new ExamAnswerKey
                {
                    TemplateId = templateId,
                    AnswersJson = json,
                    CorrectMark = request.CorrectMark,
                    IncorrectMark = request.IncorrectMark,
                    UnattemptedMark = request.UnattemptedMark,
                    UpdatedAt = DateTime.Now,
                    UpdatedBy = "Teacher"
                });
            }

            await _context.SaveChangesAsync();
            
            return Ok(new { Message = "Answer key saved successfully!" });
        }

        // ── GET /api/OMR/Dashboard ───────────────────────────────────────
        [HttpGet("dashboard")]
        public IActionResult Dashboard([FromQuery] bool showArchived = false)
        {
            var query = _context.OMRSheets.AsQueryable();

            if (!showArchived)
                query = query.Where(o => !o.IsArchived);

            var allSheets = query
                .OrderByDescending(o => o.UploadedAt)
                .ToList();

            var allKeys = _context.AnswerKeys.ToList();
            var totalSubmissions = allSheets.Count;
            var averageScore = allSheets.Any() ? allSheets.Average(s => s.MarksObtained) : 0;
            var passCount = allSheets.Count(s => {
                var config = TemplateProvider.GetTemplate(s.TemplateId);
                var key = allKeys.FirstOrDefault(k => k.TemplateId == s.TemplateId);
                int correctMark = key?.CorrectMark ?? 4;
                int maxMarks = config.TotalQuestions * correctMark;
                return s.MarksObtained >= (maxMarks * 0.3); // 30% pass mark
            });
            var passPercentage = totalSubmissions > 0 ? (passCount * 100.0 / totalSubmissions) : 0;
            var highestScore = allSheets.Any() ? allSheets.Max(s => s.MarksObtained) : 0;
            
            var templateMaxMarks = new Dictionary<string, int>();
            foreach (var key in allKeys)
            {
                var cfg = TemplateProvider.GetTemplate(key.TemplateId);
                templateMaxMarks[key.TemplateId] = cfg.TotalQuestions * key.CorrectMark;
            }

            return Ok(new {
                TotalSubmissions = totalSubmissions,
                AverageScore = Math.Round(averageScore, 2),
                PassPercentage = Math.Round(passPercentage, 1),
                HighestScore = highestScore,
                ShowArchived = showArchived,
                ArchivedCount = _context.OMRSheets.Count(o => o.IsArchived),
                TemplateMaxMarks = templateMaxMarks,
                Sheets = allSheets
            });
        }

        // ── POST /api/OMR/ResetAll ───────────────────────────────────────
        [HttpPost("resetall")]
        public async Task<IActionResult> ResetAll()
        {
            var activeSheets = await _context.OMRSheets
                .Where(o => !o.IsArchived)
                .ToListAsync();

            foreach (var sheet in activeSheets)
            {
                sheet.IsArchived = true;
                sheet.ArchivedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = $"{activeSheets.Count} result(s) reset. They are hidden but still recoverable." });
        }

        // ── POST /api/OMR/RestoreAll ─────────────────────────────────────
        [HttpPost("restoreall")]
        public async Task<IActionResult> RestoreAll()
        {
            var archivedSheets = await _context.OMRSheets
                .Where(o => o.IsArchived)
                .ToListAsync();

            foreach (var sheet in archivedSheets)
            {
                sheet.IsArchived = false;
                sheet.ArchivedAt = null;
            }

            await _context.SaveChangesAsync();
            return Ok(new { Message = $"{archivedSheets.Count} result(s) restored." });
        }

        // ── GET /api/OMR/Detail/{id} ─────────────────────────────────────
        [HttpGet("detail/{id}")]
        public async Task<IActionResult> Detail(int id)
        {
            var sheet = await _context.OMRSheets.FirstOrDefaultAsync(o => o.Id == id);
            if (sheet == null) return NotFound(new { Message = "Result not found." });

            var answerKeyRecord = await GetAnswerKeyRecordAsync(sheet.TemplateId);
            var answerKey = ParseAnswerKey(answerKeyRecord.AnswersJson);

            Dictionary<int, string> studentAnswers;
            if (string.IsNullOrWhiteSpace(sheet.AnswersJson))
                studentAnswers = new Dictionary<int, string>();
            else
                studentAnswers = JsonSerializer.Deserialize<Dictionary<int, string>>(sheet.AnswersJson)
                                 ?? new Dictionary<int, string>();

            int correct = studentAnswers.Count(a => answerKey.ContainsKey(a.Key) && a.Value == answerKey[a.Key]);
            int wrong = studentAnswers.Count(a => answerKey.ContainsKey(a.Key) && !string.IsNullOrEmpty(a.Value) && a.Value != answerKey[a.Key]);
            int unattempted = studentAnswers.Count(a => string.IsNullOrEmpty(a.Value));

            return Ok(new {
                Sheet = sheet,
                StudentAnswers = studentAnswers,
                AnswerKey = answerKey,
                Correct = correct,
                Wrong = wrong,
                Unattempted = unattempted,
                Total = answerKey.Count,
                MaxMarks = answerKey.Count * answerKeyRecord.CorrectMark
            });
        }

        // ── GET /api/OMR/List ────────────────────────────────────────────
        [HttpGet("list")]
        public IActionResult List()
        {
            var omrSheets = _context.OMRSheets
                .Where(o => !o.IsArchived)
                .OrderByDescending(o => o.UploadedAt)
                .ToList();
            return Ok(omrSheets);
        }
    }
}