using backend_cs.Data;
using backend_cs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_cs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Mp3MusicController : ControllerBase
    {
        private readonly AppDbContext _context;

        public Mp3MusicController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/Mp3Music
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Mp3MetaData>>> GetMp3Music(
            [FromQuery] string? title,
            [FromQuery] string? artist,
            [FromQuery] string? album,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Mp3MetaDatas
                .Include(m => m.Artist)
                .Include(m => m.Album)
                .Include(m => m.Genres)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(title))
                query = query.Where(m => m.Title.ToLower().Contains(title.ToLower()));
                
            if (!string.IsNullOrWhiteSpace(artist))
                query = query.Where(m => m.Artist != null && m.Artist.Name.ToLower().Contains(artist.ToLower()));
                
            if (!string.IsNullOrWhiteSpace(album))
                query = query.Where(m => m.Album != null && m.Album.Name.ToLower().Contains(album.ToLower()));

            var totalItems = await query.CountAsync();
            
            var items = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            Response.Headers.Append("X-Total-Count", totalItems.ToString());

            return Ok(items);
        }

        // GET: api/Mp3Music/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Mp3MetaData>> GetMp3Music(int id)
        {
            var mp3MetaData = await _context.Mp3MetaDatas
                .Include(m => m.Artist)
                .Include(m => m.Album)
                .Include(m => m.Genres)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (mp3MetaData == null)
            {
                return NotFound();
            }

            return mp3MetaData;
        }
    }
}
