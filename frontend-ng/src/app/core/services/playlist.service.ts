import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Playlist, PlaylistFilters, GeneratePlaylistCriteria, Track, SavePlaylistDto } from '../models/track.model';

@Injectable({
  providedIn: 'root'
})
export class PlaylistService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5000/api/playlists';

  getPlaylists(): Observable<Playlist[]> {
    return this.http.get<Playlist[]>(this.apiUrl);
  }

  getPlaylist(id: number): Observable<{ id: number, name: string, tracks: Track[] }> {
    return this.http.get<{ id: number, name: string, tracks: Track[] }>(`${this.apiUrl}/${id}`);
  }

  getFilters(): Observable<PlaylistFilters> {
    return this.http.get<PlaylistFilters>(`${this.apiUrl}/filters`);
  }

  generatePlaylist(criteria: GeneratePlaylistCriteria): Observable<Track[]> {
    return this.http.post<Track[]>(`${this.apiUrl}/generate`, criteria);
  }

  getAlternatives(criteria: GeneratePlaylistCriteria): Observable<Track[]> {
    return this.http.post<Track[]>(`${this.apiUrl}/alternatives`, criteria);
  }

  savePlaylist(dto: SavePlaylistDto): Observable<{ id: number; name: string }> {
    return this.http.post<{ id: number; name: string }>(this.apiUrl, dto);
  }
}
