import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TrackService } from '../../core/services/track.service';
import { Track } from '../../core/models/track.model';

@Component({
  selector: 'app-tracks',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './tracks.component.html',
  styleUrl: './tracks.component.css'
})
export class TracksComponent implements OnInit {
  tracks = signal<Track[]>([]);
  loading = signal(true);
  searchTitle = '';
  searchArtist = '';
  currentPage = signal(1);
  pageSize = 12;

  constructor(public trackService: TrackService) {}

  ngOnInit(): void {
    this.loadTracks();
  }

  loadTracks(): void {
    this.loading.set(true);
    this.trackService.getTracks({
      title: this.searchTitle || undefined,
      artist: this.searchArtist || undefined,
      page: this.currentPage(),
      pageSize: this.pageSize
    }).subscribe({
      next: (data) => {
        this.tracks.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  onSearch(): void {
    this.currentPage.set(1);
    this.loadTracks();
  }

  onClearSearch(): void {
    this.searchTitle = '';
    this.searchArtist = '';
    this.currentPage.set(1);
    this.loadTracks();
  }

  nextPage(): void {
    this.currentPage.update(p => p + 1);
    this.loadTracks();
  }

  prevPage(): void {
    if (this.currentPage() > 1) {
      this.currentPage.update(p => p - 1);
      this.loadTracks();
    }
  }
}
