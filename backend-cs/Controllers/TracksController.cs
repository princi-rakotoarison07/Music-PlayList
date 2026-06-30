using backend_cs.Data;
using backend_cs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend_cs.Controllers
{
    [ApiController]
    [Route("api/tracks")]
    public class TracksController : ControllerBase
    {
        private readonly ILogger<TracksController> _logger;
        private readonly IConfiguration _config;
        private readonly AppDbContext _db;

        public TracksController(ILogger<TracksController> logger, IConfiguration config, AppDbContext db)
        {
            _logger = logger;
            _config = config;
            _db = db;
        }

        // ── POST /api/tracks/metadata ──────────────────────────────────────────
        [HttpPost("metadata")]
        public async Task<IActionResult> PostMetadataBatch([FromBody] MetadataBatchRequest batch)
        {
            if (batch?.Tracks == null || batch.Tracks.Count == 0)
            {
                _logger.LogWarning("Received empty metadata batch.");
                return BadRequest("No metadata received.");
            }

            _logger.LogInformation("Processing metadata batch: {Count} tracks from {SourceDir}",
                batch.Tracks.Count, batch.SourceDir);

            int added = 0, updated = 0;

            foreach (var dto in batch.Tracks)
            {
                _logger.LogDebug("Processing track: {FileName} (Title: {Title}, Artists: {Artists}, Album: {Album})",
                    dto.FileName, dto.Title, string.Join(", ", dto.Artists), dto.Album);

                var primaryArtistName = !string.IsNullOrWhiteSpace(dto.AlbumArtist) 
                    ? dto.AlbumArtist 
                    : (dto.Artists != null && dto.Artists.Length > 0 ? dto.Artists[0] : "Unknown Artist");
                    
                // Find or create primary Album Artist for the Album relationship
                var albumArtist = await GetOrCreateArtist(primaryArtistName);
                
                // Find or create Album
                var album = await GetOrCreateAlbum(dto.Album, albumArtist.Id);
                
                // Look for existing track by server file path (unique)
                var existing = await _db.Mp3MetaDatas
                    .FirstOrDefaultAsync(m => m.FilePath == dto.FilePath);

                var track = existing ?? new Mp3MetaData();
                bool isNew = existing == null;

                // Map all properties
                track.Title = dto.Title;
                track.FileName = dto.FileName;
                track.FilePath = dto.FilePath;
                track.FileSize = dto.FileSize;
                track.AlbumId = album.Id;
                track.Language = dto.Language;
                track.Year = dto.Year > 0 ? (short?)Math.Clamp(dto.Year, 0u, (uint)short.MaxValue) : null;
                track.DurationSeconds = dto.DurationSeconds;
                track.Comment = dto.Comment;
                track.Track = dto.Track > 0 ? (short?)Math.Clamp(dto.Track, 0u, (uint)short.MaxValue) : null;
                track.BitRate = dto.BitRate;
                track.SampleRate = dto.SampleRate;
                track.Channels = (short)Math.Clamp(dto.Channels, 0, short.MaxValue);
                track.CreatedAt = dto.CreatedAt;
                track.ExtractedAt = dto.ExtractedAt;

                if (isNew)
                {
                    _db.Mp3MetaDatas.Add(track);
                    added++;
                }
                else
                {
                    updated++;
                }

                // Process Genres (many‑to‑many)
                await UpdateGenresForTrack(track, dto.Genres, isNew);

                // Process Artists (many-to-many)
                await UpdateArtistsForTrack(track, dto.Artists, isNew);
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("Metadata batch saved: {Added} new, {Updated} updated.", added, updated);
            return Ok(new { added, updated });
        }

        // ── POST /api/tracks/upload ────────────────────────────────────────────
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFile([FromForm] UploadFileRequest request)
        {
            if (request.File == null || request.File.Length == 0)
            {
                _logger.LogWarning("Upload request with no file.");
                return BadRequest("No file received.");
            }

            var uploadDir = _config["Storage:UploadDirectory"]
                ?? throw new InvalidOperationException("Missing 'Storage:UploadDirectory'.");

            Directory.CreateDirectory(uploadDir);

            var safeName = Path.GetFileName(
                string.IsNullOrWhiteSpace(request.FileName) ? request.File.FileName : request.FileName);

            if (string.IsNullOrWhiteSpace(safeName))
            {
                _logger.LogWarning("Invalid file name received.");
                return BadRequest("Invalid file name.");
            }

            var destPath = Path.GetFullPath(Path.Combine(uploadDir, safeName));

            _logger.LogInformation("Saving file: {FileName} → {DestPath}", safeName, destPath);

            await using var dest = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
            await request.File.CopyToAsync(dest);

            _logger.LogInformation("File saved: {Path}, size: {Size} bytes", destPath, dest.Length);

            return Ok(new { filePath = destPath, fileName = safeName, fileSize = dest.Length });
        }

        // ── Helper methods ────────────────────────────────────────

        private async Task<Artist> GetOrCreateArtist(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                name = "Unknown Artist";
            }

            var artist = await _db.Artists.FirstOrDefaultAsync(a => a.Name == name);
            if (artist != null) return artist;

            artist = new Artist { Name = name };
            _db.Artists.Add(artist);
            
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                _db.Entry(artist).State = EntityState.Detached;
                artist = await _db.Artists.FirstOrDefaultAsync(a => a.Name == name)
                    ?? throw new InvalidOperationException($"Failed to create or find artist '{name}'.");
            }
            return artist;
        }

        private async Task<Album> GetOrCreateAlbum(string albumName, int artistId)
        {
            if (string.IsNullOrWhiteSpace(albumName))
            {
                albumName = "Unknown Album";
            }

            var album = await _db.Albums
                .FirstOrDefaultAsync(a => a.Name == albumName && a.ArtistId == artistId);
            if (album != null) return album;

            album = new Album { Name = albumName, ArtistId = artistId };
            _db.Albums.Add(album);
            
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                _db.Entry(album).State = EntityState.Detached;
                album = await _db.Albums
                    .FirstOrDefaultAsync(a => a.Name == albumName && a.ArtistId == artistId)
                    ?? throw new InvalidOperationException($"Failed to create or find album '{albumName}'.");
            }
            return album;
        }

        private async Task UpdateGenresForTrack(Mp3MetaData track, string[]? genreNames, bool isNew)
        {
            if (!isNew)
            {
                await _db.Entry(track).Collection(t => t.Genres).LoadAsync();
                track.Genres.Clear();
            }

            if (genreNames == null || genreNames.Length == 0) return;

            foreach (var genreName in genreNames.Distinct())
            {
                if (string.IsNullOrWhiteSpace(genreName)) continue;

                var genre = await _db.Genres.FirstOrDefaultAsync(g => g.Name == genreName);
                if (genre == null)
                {
                    genre = new Genre { Name = genreName };
                    _db.Genres.Add(genre);
                    
                    try
                    {
                        await _db.SaveChangesAsync();
                    }
                    catch (DbUpdateException)
                    {
                        _db.Entry(genre).State = EntityState.Detached;
                        genre = await _db.Genres.FirstOrDefaultAsync(g => g.Name == genreName)
                            ?? throw new InvalidOperationException($"Failed to create or find genre '{genreName}'.");
                    }
                }
                track.Genres.Add(genre);
            }
        }

        private async Task UpdateArtistsForTrack(Mp3MetaData track, string[]? artistNames, bool isNew)
        {
            if (!isNew)
            {
                await _db.Entry(track).Collection(t => t.Artists).LoadAsync();
                track.Artists.Clear();
            }

            if (artistNames == null || artistNames.Length == 0) return;

            foreach (var artistName in artistNames.Distinct())
            {
                if (string.IsNullOrWhiteSpace(artistName)) continue;

                var artist = await _db.Artists.FirstOrDefaultAsync(a => a.Name == artistName);
                if (artist == null)
                {
                    artist = new Artist { Name = artistName };
                    _db.Artists.Add(artist);
                    
                    try
                    {
                        await _db.SaveChangesAsync();
                    }
                    catch (DbUpdateException)
                    {
                        _db.Entry(artist).State = EntityState.Detached;
                        artist = await _db.Artists.FirstOrDefaultAsync(a => a.Name == artistName)
                            ?? throw new InvalidOperationException($"Failed to create or find artist '{artistName}'.");
                    }
                }
                track.Artists.Add(artist);
            }
        }

        // ── GET /api/tracks/stream/{id} ────────────────────────────────────────
        [HttpGet("stream/{id}")]
        public async Task<IActionResult> StreamAudio(int id)
        {
            var track = await _db.Mp3MetaDatas.FindAsync(id);
            if (track == null || !System.IO.File.Exists(track.FilePath))
                return NotFound("Track not found or file missing.");

            var stream = new FileStream(track.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(stream, "audio/mpeg", enableRangeProcessing: true);
        }
    }
}
