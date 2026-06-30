import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { PlaylistService } from '../../../core/services/playlist.service';
import { PlaylistFilters, Track, GeneratePlaylistCriteria } from '../../../core/models/track.model';

@Component({
  selector: 'app-playlist-creator',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './playlist-creator.component.html'
})
export class PlaylistCreatorComponent implements OnInit {
  private playlistService = inject(PlaylistService);
  private router = inject(Router);

  // Flow State
  step = signal<'MODAL' | 'EDITOR'>('MODAL');
  
  // Data State
  filters = signal<PlaylistFilters>({ genres: [], artists: [], languages: [] });
  tracks = signal<Track[]>([]);
  
  // Modal Form State
  playlistName = signal<string>('Nouvelle Playlist');
  durationVal = signal<number>(60);
  durationUnit = signal<'MINUTES' | 'HOURS'>('MINUTES');
  selectedGenre = signal<string>('Tous');
  selectedLanguage = signal<string>('Toutes');
  selectedArtists = signal<string[]>([]);
  
  excludedGenres = signal<string[]>([]);
  excludedArtists = signal<string[]>([]);
  
  // Editor State
  isSaving = signal<boolean>(false);
  showAlternatives = signal<boolean>(false);
  alternatives = signal<Track[]>([]);
  trackToReplaceIndex = signal<number | null>(null);
  
  // Review Player State
  currentReviewIndex = signal<number>(0);

  ngOnInit() {
    this.playlistService.getFilters().subscribe(data => {
      this.filters.set(data);
    });
  }

  // --- Modal Flow ---
  toggleArtist(artist: string) {
    const current = this.selectedArtists();
    if (current.includes(artist)) {
      this.selectedArtists.set(current.filter(a => a !== artist));
    } else {
      this.selectedArtists.set([...current, artist]);
    }
  }

  toggleExcludedArtist(artist: string) {
    const current = this.excludedArtists();
    if (current.includes(artist)) {
      this.excludedArtists.set(current.filter(a => a !== artist));
    } else {
      this.excludedArtists.set([...current, artist]);
    }
  }

  toggleExcludedGenre(genre: string) {
    const current = this.excludedGenres();
    if (current.includes(genre)) {
      this.excludedGenres.set(current.filter(g => g !== genre));
    } else {
      this.excludedGenres.set([...current, genre]);
    }
  }

  generatePlaylist() {
    if (!this.playlistName()) return;

    let targetMins = this.durationUnit() === 'HOURS' ? this.durationVal() * 60 : this.durationVal();

    const criteria: GeneratePlaylistCriteria = {
      targetDurationMinutes: targetMins,
      genre: this.selectedGenre(),
      language: this.selectedLanguage(),
      artists: this.selectedArtists(),
      excludedGenres: this.excludedGenres(),
      excludedArtists: this.excludedArtists()
    };

    this.playlistService.generatePlaylist(criteria).subscribe(res => {
      this.tracks.set(res);
      this.currentReviewIndex.set(0);
      this.step.set('EDITOR');
    });
  }

  cancelCreation() {
    this.router.navigate(['/playlists']);
  }

  // --- Editor Flow ---
  removeTrack(index: number) {
    const t = [...this.tracks()];
    t.splice(index, 1);
    this.tracks.set(t);
    // Adjust index if we removed the current or a previous track
    const curr = this.currentReviewIndex();
    if (curr >= t.length && t.length > 0) {
      this.currentReviewIndex.set(t.length - 1);
    } else if (curr > index) {
      this.currentReviewIndex.set(curr - 1);
    }
  }

  nextTrack() {
    if (this.currentReviewIndex() < this.tracks().length - 1) {
      this.currentReviewIndex.set(this.currentReviewIndex() + 1);
    }
  }

  prevTrack() {
    if (this.currentReviewIndex() > 0) {
      this.currentReviewIndex.set(this.currentReviewIndex() - 1);
    }
  }

  get currentTrack(): Track | null {
    const t = this.tracks();
    const idx = this.currentReviewIndex();
    return t.length > 0 && idx >= 0 && idx < t.length ? t[idx] : null;
  }

  openAlternatives(index: number) {
    this.trackToReplaceIndex.set(index);
    let targetMins = this.durationUnit() === 'HOURS' ? this.durationVal() * 60 : this.durationVal();
    
    this.playlistService.getAlternatives({
      targetDurationMinutes: targetMins,
      genre: this.selectedGenre(),
      language: this.selectedLanguage(),
      artists: this.selectedArtists(),
      excludedGenres: this.excludedGenres(),
      excludedArtists: this.excludedArtists()
    }).subscribe(res => {
      // Exclude tracks already in playlist
      const currentIds = this.tracks().map(t => t.id);
      this.alternatives.set(res.filter(t => !currentIds.includes(t.id)));
      this.showAlternatives.set(true);
    });
  }

  replaceTrackWith(altTrack: Track) {
    const idx = this.trackToReplaceIndex();
    if (idx !== null) {
      const t = [...this.tracks()];
      t[idx] = altTrack;
      this.tracks.set(t);
    }
    this.closeAlternatives();
  }

  closeAlternatives() {
    this.showAlternatives.set(false);
    this.trackToReplaceIndex.set(null);
    this.alternatives.set([]);
  }

  savePlaylist() {
    if (this.tracks().length === 0) return;
    this.isSaving.set(true);

    let targetMins = this.durationUnit() === 'HOURS' ? this.durationVal() * 60 : this.durationVal();
    
    this.playlistService.savePlaylist({
      name: this.playlistName(),
      targetDurationMinutes: targetMins,
      trackIds: this.tracks().map(t => t.id)
    }).subscribe({
      next: () => {
        this.isSaving.set(false);
        this.router.navigate(['/playlists']);
      },
      error: () => this.isSaving.set(false)
    });
  }
}
