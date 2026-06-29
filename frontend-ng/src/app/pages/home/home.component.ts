import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { TrackService } from '../../core/services/track.service';
import { Track } from '../../core/models/track.model';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit {
  tracks = signal<Track[]>([]);
  loading = signal(true);
  today = new Date();

  constructor(public trackService: TrackService) {}

  ngOnInit(): void {
    this.trackService.getTracks({ page: 1, pageSize: 6 }).subscribe({
      next: (data) => {
        this.tracks.set(data);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
  }
}
