import { Routes } from '@angular/router';
import { authGuard } from './core/services/auth.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./pages/login/login.component').then(m => m.LoginComponent),
    title: 'Connexion — Music Playlist'
  },
  {
    path: '',
    loadComponent: () => import('./pages/home/home.component').then(m => m.HomeComponent),
    canActivate: [authGuard],
    title: 'Accueil — Music Playlist'
  },
  {
    path: 'tracks',
    loadComponent: () => import('./pages/tracks/tracks.component').then(m => m.TracksComponent),
    canActivate: [authGuard],
    title: 'Toutes les Chansons — Music Playlist'
  },
  {
    path: 'playlists',
    loadComponent: () => import('./pages/playlists/playlists.component').then(m => m.PlaylistsComponent),
    canActivate: [authGuard],
    title: 'Playlists — Music Playlist'
  },
  {
    path: 'playlists/new',
    loadComponent: () => import('./pages/playlists/playlist-creator/playlist-creator.component').then(m => m.PlaylistCreatorComponent),
    canActivate: [authGuard],
    title: 'Nouvelle Playlist — Music Playlist'
  },
  {
    path: 'playlists/:id',
    loadComponent: () => import('./pages/playlists/playlist-player/playlist-player.component').then(m => m.PlaylistPlayerComponent),
    canActivate: [authGuard],
    title: 'Lecture de Playlist — Music Playlist'
  },
  {
    path: 'blacklist',
    loadComponent: () => import('./pages/blacklist/blacklist.component').then(m => m.BlacklistComponent),
    canActivate: [authGuard],
    title: 'Blacklist — Music Playlist'
  },
  {
    path: '**',
    redirectTo: ''
  }
];
