export interface Artist {
  id: number;
  name: string;
}

export interface Album {
  id: number;
  name: string;
  artistId: number;
  artist?: Artist;
}

export interface Genre {
  id: number;
  name: string;
}

export interface Track {
  id: number;
  title: string;
  fileName: string;
  filePath: string;
  fileSize: number;
  language: string;
  artists: Artist[];
  albumId: number | null;
  album: Album | null;
  year: number | null;
  durationSeconds: number;
  comment: string;
  track: number | null;
  bitRate: number;
  sampleRate: number;
  channels: number;
  createdAt: string;
  extractedAt: string;
  genres: Genre[];
}

export interface Playlist {
  id: number;
  name: string;
  targetDurationSeconds: number;
  userId: number;
  createdAt: string;
  trackCount: number;
}

export interface PlaylistFilters {
  genres: string[];
  artists: string[];
  languages: string[];
}

export interface GeneratePlaylistCriteria {
  targetDurationMinutes: number;
  genre: string;
  language: string;
  artists: string[];
  excludedGenres: string[];
  excludedArtists: string[];
}

export interface SavePlaylistDto {
  name: string;
  targetDurationMinutes: number;
  userId?: number;
  trackIds: number[];
}
