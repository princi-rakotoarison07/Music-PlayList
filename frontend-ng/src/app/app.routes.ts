import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./pages/home/home.component').then(m => m.HomeComponent),
    title: 'Accueil — Music Playlist'
  },
  {
    path: 'tracks',
    loadComponent: () => import('./pages/tracks/tracks.component').then(m => m.TracksComponent),
    title: 'Toutes les Chansons — Music Playlist'
  },
  {
    path: 'playlists',
    loadComponent: () => import('./pages/playlists/playlists.component').then(m => m.PlaylistsComponent),
    title: 'Playlists — Music Playlist'
  },
  {
    path: 'playlists/new',
    loadComponent: () => import('./pages/playlists/playlist-creator/playlist-creator.component').then(m => m.PlaylistCreatorComponent),
    title: 'Nouvelle Playlist — Music Playlist'
  },
  {
    path: 'playlists/:id',
    loadComponent: () => import('./pages/playlists/playlist-player/playlist-player.component').then(m => m.PlaylistPlayerComponent),
    title: 'Lecture de Playlist — Music Playlist'
  },
  {
    path: '**',
    redirectTo: ''
  }
];
