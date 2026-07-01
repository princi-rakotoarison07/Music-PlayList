using backend_cs.Data;
using backend_cs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_cs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BlacklistController : ControllerBase
    {
        private readonly AppDbContext _db;

        public BlacklistController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetBlacklist()
        {
            var rules = await _db.BlacklistRules.OrderByDescending(r => r.CreatedAt).ToListAsync();
            return Ok(rules);
        }

        [HttpPost]
        public async Task<IActionResult> AddRule([FromBody] BlacklistRuleDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.RuleType) || string.IsNullOrWhiteSpace(dto.Value))
                return BadRequest("RuleType and Value are required.");

            // Avoid duplicates
            var exists = await _db.BlacklistRules.AnyAsync(r => r.RuleType == dto.RuleType && r.Value == dto.Value);
            if (exists) return Ok(); // Already blacklisted

            var rule = new BlacklistRule
            {
                RuleType = dto.RuleType,
                Value = dto.Value,
                CreatedAt = DateTime.UtcNow
            };

            _db.BlacklistRules.Add(rule);
            await _db.SaveChangesAsync();

            return Ok(rule);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRule(int id)
        {
            var rule = await _db.BlacklistRules.FindAsync(id);
            if (rule != null)
            {
                _db.BlacklistRules.Remove(rule);
                await _db.SaveChangesAsync();
            }
            return Ok();
        }
    }

    public class BlacklistRuleDto
    {
        public string RuleType { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }
}
