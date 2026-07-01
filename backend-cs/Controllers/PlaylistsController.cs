using backend_cs.Data;
using backend_cs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.IO.Compression;
using System.Text.Json;

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

        [HttpGet]
        public async Task<IActionResult> GetPlaylists([FromQuery] int userId)
        {
            if (userId <= 0)
            {
                Console.WriteLine($"[GetPlaylists] BadRequest: UserId is <= 0 ({userId})");
                return BadRequest("UserId is required and must be greater than 0.");
            }

            var playlists = await _db.Playlists
                .Where(p => p.UserId == userId)
                .Include(p => p.PlaylistTracks)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(playlists.Select(p => new {
                p.Id,
                p.Name,
                p.TargetDurationSeconds,
                p.UserId,
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

        // GET: api/playlists/{id}/download
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadPlaylistZip(int id)
        {
            var playlist = await _db.Playlists
                .Include(p => p.PlaylistTracks)
                    .ThenInclude(pt => pt.Mp3)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (playlist == null) return NotFound("Playlist not found.");

            var memoryStream = new MemoryStream();
            using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                foreach (var pt in playlist.PlaylistTracks.OrderBy(pt => pt.Position))
                {
                    if (pt.Mp3 != null && System.IO.File.Exists(pt.Mp3.FilePath))
                    {
                        var entryName = $"{pt.Position + 1:D2} - {pt.Mp3.FileName}";
                        var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
                        using var entryStream = entry.Open();
                        using var fileStream = new FileStream(pt.Mp3.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        await fileStream.CopyToAsync(entryStream);
                    }
                }
            }

            memoryStream.Position = 0;
            var safeName = string.Join("_", playlist.Name.Split(Path.GetInvalidFileNameChars()));
            return File(memoryStream, "application/zip", $"{safeName}.zip");
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
            int limitSeconds = GetDurationLimitSeconds();

            foreach (var track in availableTracks)
            {
                if (currentDuration + track.DurationSeconds <= targetDuration + limitSeconds)
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
            if (dto == null)
            {
                Console.WriteLine("[SavePlaylist] BadRequest: dto is null");
                return BadRequest("Playlist data is null.");
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                Console.WriteLine("[SavePlaylist] BadRequest: Name is empty");
                return BadRequest("Playlist name is required.");
            }

            if (dto.TrackIds == null || !dto.TrackIds.Any())
            {
                Console.WriteLine("[SavePlaylist] BadRequest: TrackIds is empty or null");
                return BadRequest("Playlist must contain at least one track.");
            }

            if (dto.UserId <= 0)
            {
                Console.WriteLine($"[SavePlaylist] BadRequest: UserId is <= 0 ({dto.UserId})");
                return BadRequest("UserId is required and must be greater than 0.");
            }

            var playlist = new Playlist
            {
                Name = dto.Name,
                TargetDurationSeconds = dto.TargetDurationMinutes * 60,
                UserId = dto.UserId,
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

        // POST: api/playlists/merge
        [HttpPost("merge")]
        public async Task<IActionResult> MergePlaylists([FromBody] MergePlaylistDto dto)
        {
            if (dto == null)
            {
                Console.WriteLine("[MergePlaylists] BadRequest: dto is null");
                return BadRequest("Merge data is null.");
            }

            if (string.IsNullOrWhiteSpace(dto.Name))
            {
                Console.WriteLine("[MergePlaylists] BadRequest: Name is empty");
                return BadRequest("Le nom de la playlist est requis.");
            }

            if (dto.PlaylistIds == null || dto.PlaylistIds.Count < 2)
            {
                Console.WriteLine($"[MergePlaylists] BadRequest: PlaylistIds count is < 2 ({dto.PlaylistIds?.Count})");
                return BadRequest("Sélectionnez au moins 2 playlists à fusionner.");
            }

            if (dto.UserId <= 0)
            {
                Console.WriteLine($"[MergePlaylists] BadRequest: UserId is <= 0 ({dto.UserId})");
                return BadRequest("UserId is required.");
            }

            // Collect all tracks from selected playlists (deduplicated, preserving order)
            var allTrackIds = await _db.PlaylistTracks
                .Where(pt => dto.PlaylistIds.Contains(pt.PlaylistId))
                .OrderBy(pt => pt.PlaylistId)
                .ThenBy(pt => pt.Position)
                .Select(pt => pt.Mp3Id)
                .ToListAsync();

            var uniqueTrackIds = allTrackIds.ToList(); // Removed Distinct()

            if (uniqueTrackIds.Count == 0)
                return BadRequest("Les playlists sélectionnées ne contiennent aucune chanson.");

            // Calculate total duration
            var totalDuration = await _db.Mp3MetaDatas
                .Where(m => uniqueTrackIds.Contains(m.Id))
                .SumAsync(m => m.DurationSeconds);

            var merged = new Playlist
            {
                Name = dto.Name,
                TargetDurationSeconds = totalDuration,
                UserId = dto.UserId,
                CreatedAt = DateTime.UtcNow
            };

            _db.Playlists.Add(merged);
            await _db.SaveChangesAsync();

            var tracks = new List<PlaylistTrack>();
            for (int i = 0; i < uniqueTrackIds.Count; i++)
            {
                tracks.Add(new PlaylistTrack
                {
                    PlaylistId = merged.Id,
                    Mp3Id = uniqueTrackIds[i],
                    Position = i
                });
            }

            _db.PlaylistTracks.AddRange(tracks);
            await _db.SaveChangesAsync();

            return Ok(new { merged.Id, merged.Name, TrackCount = uniqueTrackIds.Count });
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

        private int GetDurationLimitSeconds()
        {
            try
            {
                string path = Path.Combine(Directory.GetCurrentDirectory(), "..", "desktop-server-cs", "Config", "json", "limit_duree_second.json");
                if (System.IO.File.Exists(path))
                {
                    string content = System.IO.File.ReadAllText(path).Trim();
                    if (!string.IsNullOrEmpty(content))
                    {
                        if (int.TryParse(content, out int seconds))
                        {
                            return seconds;
                        }
                        
                        // Try parsing as JSON
                        using var doc = JsonDocument.Parse(content);
                        if (doc.RootElement.ValueKind == JsonValueKind.Number)
                        {
                            return doc.RootElement.GetInt32();
                        }
                        else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            if (doc.RootElement.TryGetProperty("LimitSeconds", out var prop) && prop.ValueKind == JsonValueKind.Number)
                            {
                                return prop.GetInt32();
                            }
                            if (doc.RootElement.TryGetProperty("limit", out var prop2) && prop2.ValueKind == JsonValueKind.Number)
                            {
                                return prop2.GetInt32();
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback on exception
            }
            return 300; // default 5 minutes
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
        public int UserId { get; set; }
        public List<int> TrackIds { get; set; } = new();
    }

    public class MergePlaylistDto
    {
        public string Name { get; set; } = string.Empty;
        public int UserId { get; set; }
        public List<int> PlaylistIds { get; set; } = new();
    }
}
