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
    "AlbumId"         INT REFERENCES "Albums"("Id") ON DELETE SET NULL,
    "Language"        VARCHAR(50) DEFAULT 'Unknown',
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

-- ── Mp3Artist (junction) ─────────────────────────────────────
CREATE TABLE IF NOT EXISTS "Mp3Artist" (
    "Id"       SERIAL PRIMARY KEY,
    "Mp3Id"    INT NOT NULL REFERENCES "Mp3MetaData"("Id") ON DELETE CASCADE,
    "ArtistId" INT NOT NULL REFERENCES "Artists"("Id") ON DELETE CASCADE,
    UNIQUE ("Mp3Id", "ArtistId")
);

-- ── Playlists ────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "Playlists" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(255) NOT NULL,
    "TargetDurationSeconds" INT NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ── PlaylistTracks ───────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "PlaylistTracks" (
    "Id" SERIAL PRIMARY KEY,
    "PlaylistId" INT NOT NULL REFERENCES "Playlists"("Id") ON DELETE CASCADE,
    "Mp3Id" INT NOT NULL REFERENCES "Mp3MetaData"("Id") ON DELETE CASCADE,
    "Position" INT NOT NULL DEFAULT 0,
    UNIQUE ("PlaylistId", "Mp3Id")
);

-- ── BlacklistRules ───────────────────────────────────────────
CREATE TABLE IF NOT EXISTS "BlacklistRules" (
    "Id" SERIAL PRIMARY KEY,
    "RuleType" VARCHAR(50) NOT NULL, -- e.g., 'Artist', 'Genre', 'Title'
    "Value" VARCHAR(255) NOT NULL,
    "CreatedAt" TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE ("RuleType", "Value")
);

-- ── Indexes ──────────────────────────────────────────────────
CREATE INDEX IF NOT EXISTS "idx_mp3_album"    ON "Mp3MetaData"("AlbumId");
CREATE INDEX IF NOT EXISTS "idx_mp3_title"    ON "Mp3MetaData"("Title");
CREATE INDEX IF NOT EXISTS "idx_mp3_filename" ON "Mp3MetaData"("FileName");
CREATE INDEX IF NOT EXISTS "idx_mp3_language" ON "Mp3MetaData"("Language");
CREATE INDEX IF NOT EXISTS "idx_albums_artist" ON "Albums"("ArtistId");

-- ── Migrations pour base existante ───────────────────────────
ALTER TABLE "Mp3MetaData" ADD COLUMN IF NOT EXISTS "Language" VARCHAR(50) DEFAULT 'Unknown';

-- Migrer l'ancien ArtistId s'il existe encore (et si Mp3Artist est vide)
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='Mp3MetaData' AND column_name='ArtistId') THEN
        INSERT INTO "Mp3Artist" ("Mp3Id", "ArtistId")
        SELECT "Id", "ArtistId" FROM "Mp3MetaData" WHERE "ArtistId" IS NOT NULL
        ON CONFLICT DO NOTHING;
        
        ALTER TABLE "Mp3MetaData" DROP COLUMN "ArtistId";
    END IF;
END $$;