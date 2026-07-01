import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <div class="min-h-screen bg-peach-light/20 flex items-center justify-center py-12 px-4 sm:px-6 lg:px-8">
      <div class="max-w-md w-full space-y-8 bg-white p-10 rounded-3xl shadow-xl border border-peach-light">
        <div>
          <h2 class="mt-6 text-center text-3xl font-extrabold text-text-primary">
            Bienvenue sur MusicPlay
          </h2>
          <p class="mt-2 text-center text-sm text-text-muted">
            Connectez-vous pour gerer vos playlists
          </p>
        </div>
        <form class="mt-8 space-y-6" (ngSubmit)="onSubmit()">
          
          @if (errorMessage) {
            <div class="p-4 bg-red-50 border border-red-200 rounded-xl text-red-600 text-sm font-medium">
              {{ errorMessage }}
            </div>
          }

          <div class="space-y-4">
            <div>
              <label for="email" class="block text-sm font-medium text-text-secondary mb-1">Adresse Email</label>
              <input id="email" name="email" type="email" required [(ngModel)]="email"
                class="appearance-none relative block w-full px-4 py-3 border border-peach-light bg-alabaster placeholder-gray-400 text-text-primary rounded-xl focus:outline-none focus:ring-2 focus:ring-peach focus:border-peach focus:z-10 sm:text-sm transition-all"
                placeholder="admin@gmail.com">
            </div>
            <div>
              <label for="password" class="block text-sm font-medium text-text-secondary mb-1">Mot de passe</label>
              <input id="password" name="password" type="password" required [(ngModel)]="password"
                class="appearance-none relative block w-full px-4 py-3 border border-peach-light bg-alabaster placeholder-gray-400 text-text-primary rounded-xl focus:outline-none focus:ring-2 focus:ring-peach focus:border-peach focus:z-10 sm:text-sm transition-all"
                placeholder="admin123">
            </div>
          </div>

          <div>
            <button type="submit" [disabled]="isLoading"
              class="group relative w-full flex justify-center py-3 px-4 border border-transparent text-sm font-semibold rounded-xl text-white bg-peach hover:bg-peach-dark focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-peach transition-all shadow-md shadow-peach/30 disabled:opacity-70">
              {{ isLoading ? 'Connexion en cours...' : 'Se connecter' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  `
})
export class LoginComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  email = 'admin@gmail.com';
  password = 'admin123';
  errorMessage = '';
  isLoading = false;

  onSubmit() {
    this.isLoading = true;
    this.errorMessage = '';
    
    this.authService.login({ email: this.email, password: this.password }).subscribe({
      next: () => {
        this.router.navigate(['/']);
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = err.error?.message || 'Identifiants incorrects.';
      }
    });
  }
}
