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
  artistId: number | null;
  artist: Artist | null;
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
