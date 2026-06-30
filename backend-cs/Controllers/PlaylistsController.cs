using backend_cs.Data;
using backend_cs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_cs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlaylistsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public PlaylistsController(AppDbContext db)
        {
            _db = db;
        }

        // GET: api/playlists
        [HttpGet]
        public async Task<IActionResult> GetPlaylists()
        {
            var playlists = await _db.Playlists
                .Include(p => p.PlaylistTracks)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(playlists.Select(p => new {
                p.Id,
                p.Name,
                p.TargetDurationSeconds,
                p.CreatedAt,
                TrackCount = p.PlaylistTracks.Count
            }));
        }

        // GET: api/playlists/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPlaylist(int id)
        {
            var playlist = await _db.Playlists
                .Include(p => p.PlaylistTracks)
                    .ThenInclude(pt => pt.Mp3)
                        .ThenInclude(m => m.Artists)
                .Include(p => p.PlaylistTracks)
                    .ThenInclude(pt => pt.Mp3)
                        .ThenInclude(m => m.Album)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (playlist == null) return NotFound();

            var tracks = playlist.PlaylistTracks
                .OrderBy(pt => pt.Position)
                .Select(pt => MapToDto(pt.Mp3));

            return Ok(new {
                playlist.Id,
                playlist.Name,
                Tracks = tracks
            });
        }

        // POST: api/playlists/generate
        [HttpPost("generate")]
        public async Task<IActionResult> GeneratePlaylist([FromBody] GeneratePlaylistDto criteria)
        {
            var query = _db.Mp3MetaDatas
                .Include(m => m.Artists)
                .Include(m => m.Genres)
                .Include(m => m.Album)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(criteria.Language) && criteria.Language != "Toutes")
            {
                query = query.Where(m => m.Language == criteria.Language);
            }

            if (!string.IsNullOrWhiteSpace(criteria.Genre) && criteria.Genre != "Tous")
            {
                query = query.Where(m => m.Genres.Any(g => g.Name == criteria.Genre));
            }

            if (criteria.Artists != null && criteria.Artists.Any())
            {
                query = query.Where(m => m.Artists.Any(a => criteria.Artists.Contains(a.Name)));
            }

            // Exclusions
            if (criteria.ExcludedGenres != null && criteria.ExcludedGenres.Any())
            {
                query = query.Where(m => !m.Genres.Any(g => criteria.ExcludedGenres.Contains(g.Name)));
            }

            if (criteria.ExcludedArtists != null && criteria.ExcludedArtists.Any())
            {
                query = query.Where(m => !m.Artists.Any(a => criteria.ExcludedArtists.Contains(a.Name)));
            }

            // Shuffle and fetch
            var availableTracks = await query.ToListAsync();
            var rng = new Random();
            availableTracks = availableTracks.OrderBy(x => rng.Next()).ToList();

            var selectedTracks = new List<Mp3MetaData>();
            int currentDuration = 0;
            int targetDuration = criteria.TargetDurationMinutes * 60;

            foreach (var track in availableTracks)
            {
                if (currentDuration + track.DurationSeconds <= targetDuration + 180) // allow up to 3 mins overflow
                {
                    selectedTracks.Add(track);
                    currentDuration += track.DurationSeconds;
                }
                
                if (currentDuration >= targetDuration)
                    break;
            }

            return Ok(selectedTracks.Select(MapToDto));
        }

        // POST: api/playlists/alternatives
        [HttpPost("alternatives")]
        public async Task<IActionResult> GetAlternatives([FromBody] GeneratePlaylistDto criteria)
        {
            var query = _db.Mp3MetaDatas
                .Include(m => m.Artists)
                .Include(m => m.Genres)
                .Include(m => m.Album)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(criteria.Language) && criteria.Language != "Toutes")
                query = query.Where(m => m.Language == criteria.Language);

            if (!string.IsNullOrWhiteSpace(criteria.Genre) && criteria.Genre != "Tous")
                query = query.Where(m => m.Genres.Any(g => g.Name == criteria.Genre));

            if (criteria.Artists != null && criteria.Artists.Any())
                query = query.Where(m => m.Artists.Any(a => criteria.Artists.Contains(a.Name)));

            // Exclusions
            if (criteria.ExcludedGenres != null && criteria.ExcludedGenres.Any())
                query = query.Where(m => !m.Genres.Any(g => criteria.ExcludedGenres.Contains(g.Name)));

            if (criteria.ExcludedArtists != null && criteria.ExcludedArtists.Any())
                query = query.Where(m => !m.Artists.Any(a => criteria.ExcludedArtists.Contains(a.Name)));

            // Just return top 10 random matches
            var tracks = await query.ToListAsync();
            var rng = new Random();
            var alternatives = tracks.OrderBy(x => rng.Next()).Take(10).ToList();

            return Ok(alternatives.Select(MapToDto));
        }

        // POST: api/playlists
        [HttpPost]
        public async Task<IActionResult> SavePlaylist([FromBody] SavePlaylistDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || dto.TrackIds == null || !dto.TrackIds.Any())
                return BadRequest("Invalid playlist data.");

            var playlist = new Playlist
            {
                Name = dto.Name,
                TargetDurationSeconds = dto.TargetDurationMinutes * 60,
                CreatedAt = DateTime.UtcNow
            };

            _db.Playlists.Add(playlist);
            await _db.SaveChangesAsync();

            var tracks = new List<PlaylistTrack>();
            for (int i = 0; i < dto.TrackIds.Count; i++)
            {
                tracks.Add(new PlaylistTrack
                {
                    PlaylistId = playlist.Id,
                    Mp3Id = dto.TrackIds[i],
                    Position = i
                });
            }

            _db.PlaylistTracks.AddRange(tracks);
            await _db.SaveChangesAsync();

            return Ok(new { playlist.Id, playlist.Name });
        }

        // Helper endpoints for filters
        [HttpGet("filters")]
        public async Task<IActionResult> GetFilters()
        {
            var genres = await _db.Genres.Select(g => g.Name).OrderBy(n => n).ToListAsync();
            var artists = await _db.Artists.Select(a => a.Name).OrderBy(n => n).ToListAsync();
            var languages = await _db.Mp3MetaDatas.Select(m => m.Language).Distinct().OrderBy(l => l).ToListAsync();

            return Ok(new { genres, artists, languages });
        }

        private object MapToDto(Mp3MetaData track)
        {
            return new
            {
                track.Id,
                track.Title,
                track.FileName,
                track.DurationSeconds,
                track.Language,
                Artists = track.Artists.Select(a => new { a.Id, a.Name }),
                Genres = track.Genres.Select(g => new { g.Id, g.Name }),
                Album = track.Album == null ? null : new { track.Album.Id, track.Album.Name }
            };
        }
    }

    public class GeneratePlaylistDto
    {
        public int TargetDurationMinutes { get; set; }
        public string Genre { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public List<string> Artists { get; set; } = new();
        public List<string> ExcludedGenres { get; set; } = new();
        public List<string> ExcludedArtists { get; set; } = new();
    }

    public class SavePlaylistDto
    {
        public string Name { get; set; } = string.Empty;
        public int TargetDurationMinutes { get; set; }
        public List<int> TrackIds { get; set; } = new();
    }
}
