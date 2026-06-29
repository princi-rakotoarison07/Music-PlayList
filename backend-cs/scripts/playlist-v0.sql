-- ============================================================
--  Playlist Project — PostgreSQL Schema
-- ============================================================

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- ── Genres ──────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "Genres" (
    "Id"    SERIAL PRIMARY KEY,
    "Name"  VARCHAR(100) NOT NULL UNIQUE
);

-- ── Artists ─────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "Artists" (
    "Id"    SERIAL PRIMARY KEY,
    "Name"  VARCHAR(255) NOT NULL UNIQUE
);

-- ── Albums ──────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "Albums" (
    "Id"        SERIAL PRIMARY KEY,
    "Name"      VARCHAR(255) NOT NULL,
    "ArtistId"  INT NOT NULL REFERENCES "Artists"("Id") ON DELETE CASCADE,
    UNIQUE ("Name", "ArtistId")
);

-- ── Mp3MetaData ──────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "Mp3MetaData" (
    "Id"              SERIAL PRIMARY KEY,
    "Title"           VARCHAR(500) NOT NULL DEFAULT '',
    "FileName"        VARCHAR(500) NOT NULL,
    "FilePath"        VARCHAR(1000) NOT NULL UNIQUE,
    "FileSize"        BIGINT NOT NULL DEFAULT 0,
    "ArtistId"        INT REFERENCES "Artists"("Id") ON DELETE SET NULL,
    "AlbumId"         INT REFERENCES "Albums"("Id") ON DELETE SET NULL,
    "Year"            SMALLINT,
    "DurationSeconds" INT NOT NULL DEFAULT 0,
    "Comment"         TEXT DEFAULT '',
    "Track"           SMALLINT,
    "BitRate"         INT NOT NULL DEFAULT 0,
    "SampleRate"      INT NOT NULL DEFAULT 0,
    "Channels"        SMALLINT NOT NULL DEFAULT 0,
    "CreatedAt"       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ExtractedAt"     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ── Mp3Genre (junction) ──────────────────────────────────────
CREATE TABLE IF NOT EXISTS "Mp3Genre" (
    "Id"      SERIAL PRIMARY KEY,
    "Mp3Id"   INT NOT NULL REFERENCES "Mp3MetaData"("Id") ON DELETE CASCADE,
    "GenreId" INT NOT NULL REFERENCES "Genres"("Id") ON DELETE CASCADE,
    UNIQUE ("Mp3Id", "GenreId")
);

-- ── Indexes ──────────────────────────────────────────────────
CREATE INDEX IF NOT EXISTS "idx_mp3_artist"   ON "Mp3MetaData"("ArtistId");
CREATE INDEX IF NOT EXISTS "idx_mp3_album"    ON "Mp3MetaData"("AlbumId");
CREATE INDEX IF NOT EXISTS "idx_mp3_title"    ON "Mp3MetaData"("Title");
CREATE INDEX IF NOT EXISTS "idx_mp3_filename" ON "Mp3MetaData"("FileName");
CREATE INDEX IF NOT EXISTS "idx_albums_artist" ON "Albums"("ArtistId");