import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { PlaylistService } from '../../core/services/playlist.service';
import { Playlist } from '../../core/models/track.model';

@Component({
  selector: 'app-playlists',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <section class="max-w-[1200px] mx-auto px-6 pb-16">
      <div class="pt-10 pb-6 flex items-center justify-between">
        <div>
          <h1 class="text-[28px] font-bold text-text-primary tracking-tight mb-1.5">Mes Playlists</h1>
          <p class="text-sm text-text-muted">Gérez vos sélections musicales personnalisées</p>
        </div>
        <a routerLink="/playlists/new"
           class="inline-flex items-center gap-2 px-5 py-2.5 bg-sage text-white text-[13px] font-semibold
                  rounded-lg hover:bg-sage-dark hover:-translate-y-0.5 hover:shadow-lg hover:shadow-sage/30 transition-all duration-200">
          <svg class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <line x1="12" y1="5" x2="12" y2="19"/>
            <line x1="5" y1="12" x2="19" y2="12"/>
          </svg>
          Ajouter une nouvelle playlist
        </a>
      </div>

      @if (loading()) {
        <div class="text-center py-16 text-text-muted">
          <div class="w-9 h-9 border-3 border-peach-light border-t-sage rounded-full mx-auto mb-4 animate-spin"></div>
          <p>Chargement des playlists…</p>
        </div>
      }

      @if (!loading() && playlists().length === 0) {
        <div class="text-center py-16 bg-alabaster border border-peach-light rounded-xl">
          <div class="w-16 h-16 mx-auto mb-4 rounded-2xl bg-peach-light flex items-center justify-center">
            <svg class="w-8 h-8 text-peach" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01"/>
            </svg>
          </div>
          <h2 class="text-xl font-semibold text-text-primary mb-2">Aucune playlist</h2>
          <p class="text-text-muted text-sm mb-6">Créez votre première playlist dès maintenant.</p>
        </div>
      }

      @if (!loading() && playlists().length > 0) {
        <div class="grid grid-cols-1 md:grid-cols-3 gap-6">
          @for (playlist of playlists(); track playlist.id) {
            <div class="p-5 bg-alabaster border border-peach-light rounded-xl hover:-translate-y-1 hover:shadow-xl hover:border-peach transition-all duration-300 relative group">
              <div class="flex justify-between items-start mb-4">
                <div class="w-12 h-12 rounded-lg bg-gradient-to-br from-peach to-sage flex items-center justify-center text-white">
                  <svg class="w-6 h-6" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                    <path d="M12 3v10.55c-.59-.34-1.27-.55-2-.55-2.21 0-4 1.79-4 4s1.79 4 4 4 4-1.79 4-4V7h4V3h-6z"/>
                  </svg>
                </div>
                <!-- Play Actions -->
                <div class="flex gap-2 opacity-0 group-hover:opacity-100 transition-opacity">
                  <a [routerLink]="['/playlists', playlist.id]" class="p-2 bg-sage-dark text-white rounded-full hover:scale-110 transition-transform shadow-md cursor-pointer" title="Lecture normale">
                    <svg class="w-4 h-4 ml-0.5" viewBox="0 0 24 24" fill="currentColor">
                      <polygon points="5 3 19 12 5 21 5 3"></polygon>
                    </svg>
                  </a>
                  <a [routerLink]="['/playlists', playlist.id]" [queryParams]="{shuffle: 'true'}" class="p-2 bg-peach text-white rounded-full hover:scale-110 transition-transform shadow-md cursor-pointer" title="Lecture aléatoire">
                    <svg class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round">
                      <polyline points="16 3 21 3 21 8"></polyline>
                      <line x1="4" y1="20" x2="21" y2="3"></line>
                      <polyline points="21 16 21 21 16 21"></polyline>
                      <line x1="15" y1="15" x2="21" y2="21"></line>
                      <line x1="4" y1="4" x2="9" y2="9"></line>
                    </svg>
                  </a>
                </div>
              </div>
              <h3 class="text-lg font-bold text-text-primary mb-1">{{ playlist.name }}</h3>
              <p class="text-sm text-text-muted mb-4">{{ playlist.trackCount }} chansons • {{ formatDuration(playlist.targetDurationSeconds) }}</p>
              <div class="text-xs text-text-secondary">Créée le {{ playlist.createdAt | date:'longDate' }}</div>
            </div>
          }
        </div>
      }
    </section>
  `
})
export class PlaylistsComponent implements OnInit {
  private playlistService = inject(PlaylistService);
  
  playlists = signal<Playlist[]>([]);
  loading = signal<boolean>(true);

  ngOnInit() {
    this.loadPlaylists();
  }

  loadPlaylists() {
    this.playlistService.getPlaylists().subscribe({
      next: (data) => {
        this.playlists.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }

  formatDuration(seconds: number): string {
    const m = Math.floor(seconds / 60);
    return m > 60 ? `${Math.floor(m / 60)}h ${m % 60}m` : `${m} min`;
  }
}
