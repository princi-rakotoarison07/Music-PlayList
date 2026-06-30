import { Component, OnInit, inject, signal, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { PlaylistService } from '../../../core/services/playlist.service';
import { Track } from '../../../core/models/track.model';

@Component({
  selector: 'app-playlist-player',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <section class="max-w-[1200px] mx-auto px-6 pb-16 pt-10">
      
      @if (loading()) {
        <div class="text-center py-16 text-text-muted">
          <div class="w-9 h-9 border-3 border-peach-light border-t-sage rounded-full mx-auto mb-4 animate-spin"></div>
          <p>Chargement de la playlist…</p>
        </div>
      } @else if (error()) {
        <div class="text-center py-16 bg-red-50 border border-red-100 text-red-600 rounded-xl">
          <p>Erreur lors du chargement de la playlist.</p>
          <a routerLink="/playlists" class="mt-4 inline-block underline">Retour</a>
        </div>
      } @else {
        <div class="flex items-center justify-between mb-8">
          <div>
            <a routerLink="/playlists" class="text-xs font-semibold text-text-muted hover:text-text-primary uppercase tracking-wider mb-2 inline-flex items-center gap-1 transition-colors">
              <svg class="w-3 h-3" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="15 18 9 12 15 6"></polyline></svg>
              Retour aux playlists
            </a>
            <h1 class="text-[28px] font-bold text-text-primary tracking-tight mb-1">{{ playlistName() }}</h1>
            <p class="text-sm text-text-muted">{{ tracks().length }} chansons</p>
          </div>
          <button (click)="toggleShuffle()" [class.bg-sage]="isShuffle()" [class.text-white]="isShuffle()" [class.bg-white]="!isShuffle()" [class.text-text-secondary]="!isShuffle()" class="px-4 py-2 border border-peach-light text-[13px] font-medium rounded-lg transition-colors cursor-pointer flex items-center gap-2 shadow-sm">
            <svg class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
              <polyline points="16 3 21 3 21 8"></polyline><line x1="4" y1="20" x2="21" y2="3"></line><polyline points="21 16 21 21 16 21"></polyline><line x1="15" y1="15" x2="21" y2="21"></line><line x1="4" y1="4" x2="9" y2="9"></line>
            </svg>
            Mode Aléatoire
          </button>
        </div>

        <!-- Active Player -->
        @if (currentTrack) {
          <div class="mb-8 p-6 bg-white border-2 border-sage/40 rounded-2xl shadow-xl shadow-sage/10 relative animate-fade-in-up">
            <div class="flex items-center gap-6">
              <div class="w-20 h-20 bg-gradient-to-br from-peach to-sage rounded-xl flex items-center justify-center shrink-0 shadow-md">
                <svg class="w-10 h-10 text-white" viewBox="0 0 24 24" fill="currentColor">
                  <path d="M12 3v10.55c-.59-.34-1.27-.55-2-.55-2.21 0-4 1.79-4 4s1.79 4 4 4 4-1.79 4-4V7h4V3h-6z"/>
                </svg>
              </div>
              <div class="flex-1 min-w-0">
                <div class="text-xs font-semibold text-sage-dark uppercase tracking-wider mb-1">
                  En lecture • Piste {{ currentIndex() + 1 }} sur {{ tracks().length }}
                </div>
                <h2 class="text-xl font-bold text-text-primary truncate">{{ currentTrack.title || currentTrack.fileName }}</h2>
                <p class="text-sm text-text-muted mt-1">
                  @for (a of currentTrack.artists; track a.id; let last = $last) {
                    {{ a.name }}{{ !last ? ', ' : '' }}
                  }
                </p>
              </div>
              <div class="shrink-0 w-[300px]">
                <audio #audioPlayer controls class="w-full h-10 outline-none" [src]="'http://localhost:5000/api/tracks/stream/' + currentTrack.id" preload="auto" (ended)="nextTrack()"></audio>
              </div>
            </div>
            
            <div class="mt-6 pt-4 border-t border-peach-light flex justify-center gap-4">
              <button (click)="prevTrack()" [disabled]="currentIndex() === 0 && !isShuffle()" class="p-2 text-text-secondary hover:text-sage-dark disabled:opacity-30 transition-colors cursor-pointer bg-alabaster rounded-full">
                <svg class="w-6 h-6" viewBox="0 0 24 24" fill="currentColor"><path d="M6 6h2v12H6zm3.5 6l8.5 6V6z"/></svg>
              </button>
              <button (click)="nextTrack()" class="p-2 text-text-secondary hover:text-sage-dark transition-colors cursor-pointer bg-alabaster rounded-full">
                <svg class="w-6 h-6" viewBox="0 0 24 24" fill="currentColor"><path d="M6 18l8.5-6L6 6v12zM16 6v12h2V6h-2z"/></svg>
              </button>
            </div>
          </div>
        }

        <!-- Track List -->
        <div class="bg-white border border-peach-light rounded-xl overflow-hidden shadow-sm">
          <table class="w-full text-left border-collapse">
            <thead>
              <tr class="bg-alabaster border-b border-peach-light">
                <th class="px-5 py-3 text-[11px] font-semibold uppercase tracking-wider text-text-muted w-12 text-center">#</th>
                <th class="px-5 py-3 text-[11px] font-semibold uppercase tracking-wider text-text-muted">Titre</th>
                <th class="px-5 py-3 text-[11px] font-semibold uppercase tracking-wider text-text-muted">Artistes</th>
                <th class="px-5 py-3 text-[11px] font-semibold uppercase tracking-wider text-text-muted">Langue</th>
              </tr>
            </thead>
            <tbody>
              @for (track of tracks(); track track.id; let i = $index) {
                <tr (click)="playTrack(i)" 
                    [class.bg-sage]="i === currentIndex()"
                    [class.bg-opacity-10]="i === currentIndex()"
                    class="border-b border-peach-light/50 last:border-b-0 hover:bg-peach-light/20 transition-colors cursor-pointer">
                  <td class="px-5 py-3 text-[13px] font-medium text-text-muted text-center">
                    @if (i === currentIndex()) {
                      <div class="w-3 h-3 rounded-full bg-sage-dark mx-auto animate-pulse"></div>
                    } @else {
                      {{ i + 1 }}
                    }
                  </td>
                  <td class="px-5 py-3 text-[13px] font-medium" [class.text-sage-dark]="i === currentIndex()" [class.text-text-primary]="i !== currentIndex()">{{ track.title || track.fileName }}</td>
                  <td class="px-5 py-3 text-[13px] text-text-muted">
                    @for (a of track.artists; track a.id; let last = $last) {
                      {{ a.name }}{{ !last ? ', ' : '' }}
                    }
                  </td>
                  <td class="px-5 py-3 text-[13px] text-text-muted">{{ track.language }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>
      }
    </section>
  `
})
export class PlaylistPlayerComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private playlistService = inject(PlaylistService);

  @ViewChild('audioPlayer') audioPlayerRef!: ElementRef<HTMLAudioElement>;

  playlistName = signal<string>('');
  tracks = signal<Track[]>([]);
  originalTracks: Track[] = [];
  loading = signal<boolean>(true);
  error = signal<boolean>(false);
  
  currentIndex = signal<number>(0);
  isShuffle = signal<boolean>(false);

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    const shuffleParam = this.route.snapshot.queryParamMap.get('shuffle');
    if (shuffleParam === 'true') {
      this.isShuffle.set(true);
    }
    
    if (id) {
      this.playlistService.getPlaylist(Number(id)).subscribe({
        next: (data) => {
          this.playlistName.set(data.name);
          this.originalTracks = [...data.tracks];
          
          if (this.isShuffle()) {
            this.tracks.set([...data.tracks].sort(() => Math.random() - 0.5));
          } else {
            this.tracks.set(data.tracks);
          }
          
          this.loading.set(false);
          // Auto-play the first track after view initializes
          setTimeout(() => this.playAudio(), 100);
        },
        error: () => {
          this.error.set(true);
          this.loading.set(false);
        }
      });
    }
  }

  get currentTrack(): Track | null {
    const t = this.tracks();
    const idx = this.currentIndex();
    return t.length > 0 && idx >= 0 && idx < t.length ? t[idx] : null;
  }

  playTrack(index: number) {
    this.currentIndex.set(index);
    setTimeout(() => this.playAudio(), 50);
  }

  nextTrack() {
    if (this.currentIndex() < this.tracks().length - 1) {
      this.currentIndex.set(this.currentIndex() + 1);
    } else {
      // Loop back to start or re-shuffle if in shuffle mode
      if (this.isShuffle()) {
        this.tracks.set([...this.originalTracks].sort(() => Math.random() - 0.5));
      }
      this.currentIndex.set(0);
    }
    setTimeout(() => this.playAudio(), 50);
  }

  prevTrack() {
    if (this.currentIndex() > 0) {
      this.currentIndex.set(this.currentIndex() - 1);
      setTimeout(() => this.playAudio(), 50);
    }
  }

  toggleShuffle() {
    this.isShuffle.set(!this.isShuffle());
    const currentActive = this.currentTrack;
    
    if (this.isShuffle()) {
      const shuffled = [...this.originalTracks].sort(() => Math.random() - 0.5);
      this.tracks.set(shuffled);
    } else {
      this.tracks.set([...this.originalTracks]);
    }
    
    // Find the track we were playing and update index
    if (currentActive) {
      const newIndex = this.tracks().findIndex(t => t.id === currentActive.id);
      if (newIndex !== -1) {
        this.currentIndex.set(newIndex);
      } else {
        this.currentIndex.set(0);
      }
    }
  }

  private playAudio() {
    if (this.audioPlayerRef && this.audioPlayerRef.nativeElement) {
      this.audioPlayerRef.nativeElement.play().catch(e => console.log('Autoplay prevented:', e));
    }
  }
}
