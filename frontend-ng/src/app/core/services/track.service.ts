import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Track } from '../models/track.model';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class TrackService {

  private readonly apiUrl = `${environment.apiBaseUrl}/mp3music`;

  constructor(private http: HttpClient) {}

  getTracks(params?: {
    title?: string;
    artist?: string;
    album?: string;
    page?: number;
    pageSize?: number;
  }): Observable<Track[]> {
    let httpParams = new HttpParams();

    if (params?.title) httpParams = httpParams.set('title', params.title);
    if (params?.artist) httpParams = httpParams.set('artist', params.artist);
    if (params?.album) httpParams = httpParams.set('album', params.album);
    if (params?.page) httpParams = httpParams.set('page', params.page.toString());
    if (params?.pageSize) httpParams = httpParams.set('pageSize', params.pageSize.toString());

    return this.http.get<Track[]>(this.apiUrl, { params: httpParams });
  }

  getTrackById(id: number): Observable<Track> {
    return this.http.get<Track>(`${this.apiUrl}/${id}`);
  }

  /** Format duration from seconds to mm:ss */
  formatDuration(seconds: number): string {
    const mins = Math.floor(seconds / 60);
    const secs = seconds % 60;
    return `${mins}:${secs.toString().padStart(2, '0')}`;
  }

  /** Format file size to human-readable */
  formatFileSize(bytes: number): string {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
  }
}
