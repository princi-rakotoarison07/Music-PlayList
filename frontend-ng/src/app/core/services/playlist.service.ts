import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { Playlist, PlaylistFilters, GeneratePlaylistCriteria, Track, SavePlaylistDto } from '../models/track.model';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class PlaylistService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private apiUrl = 'http://localhost:5000/api/playlists';

  private get userId(): number {
    return this.authService.currentUser?.id ?? 0;
  }

  getPlaylists(): Observable<Playlist[]> {
    if (this.userId === 0) {
      return of([]);
    }
    return this.http.get<Playlist[]>(`${this.apiUrl}?userId=${this.userId}`);
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
    return this.http.post<{ id: number; name: string }>(this.apiUrl, { ...dto, userId: this.userId });
  }

  mergePlaylists(name: string, playlistIds: number[]): Observable<{ id: number; name: string; trackCount: number }> {
    return this.http.post<{ id: number; name: string; trackCount: number }>(`${this.apiUrl}/merge`, {
      name,
      playlistIds,
      userId: this.userId
    });
  }
}
