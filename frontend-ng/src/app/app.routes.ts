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
    path: '**',
    redirectTo: ''
  }
];
