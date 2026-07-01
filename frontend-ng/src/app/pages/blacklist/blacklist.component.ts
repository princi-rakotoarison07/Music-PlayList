import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BlacklistService, BlacklistRule } from '../../core/services/blacklist.service';

@Component({
  selector: 'app-blacklist',
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="max-w-[1000px] mx-auto px-6 pb-16 pt-10 animate-fade-in-up">
      <div class="mb-8">
        <h1 class="text-[28px] font-bold text-text-primary tracking-tight mb-1">Liste Noire (Blacklist)</h1>
        <p class="text-sm text-text-muted">Configurez les artistes, genres ou titres à rejeter automatiquement lors de l'importation.</p>
      </div>

      <!-- Add Rule Form -->
      <div class="bg-white border-2 border-red-100 rounded-2xl p-6 mb-8 shadow-sm">
        <h2 class="text-sm font-semibold text-red-600 uppercase tracking-wider mb-4">Ajouter une nouvelle exclusion</h2>
        <div class="flex gap-4 items-end">
          <div class="w-48">
            <label class="block text-xs font-medium text-text-muted mb-2">Type d'exclusion</label>
            <select [(ngModel)]="newRuleType" class="w-full px-4 py-2.5 border border-peach-light rounded-xl text-sm bg-alabaster outline-none focus:border-red-300 focus:ring-3 focus:ring-red-100 cursor-pointer transition-all">
              <option value="Artist">Artiste</option>
              <option value="Genre">Genre</option>
              <option value="Title">Titre de chanson</option>
            </select>
          </div>
          <div class="flex-1">
            <label class="block text-xs font-medium text-text-muted mb-2">Valeur (ex: "Stromae")</label>
            <input type="text" [(ngModel)]="newRuleValue" placeholder="Nom exact à exclure..." (keyup.enter)="addRule()"
                   class="w-full px-4 py-2.5 border border-peach-light rounded-xl text-sm bg-alabaster outline-none focus:border-red-300 focus:ring-3 focus:ring-red-100 transition-all" />
          </div>
          <button (click)="addRule()" [disabled]="!newRuleValue()" class="px-6 py-2.5 bg-red-500 text-white text-sm font-semibold rounded-xl hover:bg-red-600 disabled:opacity-50 transition-colors cursor-pointer shadow-md shadow-red-500/20 shrink-0">
            Ajouter à la liste noire
          </button>
        </div>
      </div>

      <!-- Rules List -->
      <div class="bg-white border border-peach-light rounded-xl overflow-hidden shadow-sm">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="bg-alabaster border-b border-peach-light">
              <th class="px-5 py-3 text-[11px] font-semibold uppercase tracking-wider text-text-muted">Type</th>
              <th class="px-5 py-3 text-[11px] font-semibold uppercase tracking-wider text-text-muted">Valeur bloquée</th>
              <th class="px-5 py-3 text-[11px] font-semibold uppercase tracking-wider text-text-muted text-right">Action</th>
            </tr>
          </thead>
          <tbody>
            @for (rule of rules(); track rule.id) {
              <tr class="border-b border-peach-light/50 last:border-b-0 hover:bg-red-50/50 transition-colors">
                <td class="px-5 py-3">
                  <span class="px-2.5 py-1 text-[11px] font-bold rounded-md bg-red-100 text-red-700 uppercase tracking-wider">
                    {{ rule.ruleType === 'Artist' ? 'Artiste' : (rule.ruleType === 'Genre' ? 'Genre' : 'Titre') }}
                  </span>
                </td>
                <td class="px-5 py-3 text-[13px] font-medium text-text-primary">{{ rule.value }}</td>
                <td class="px-5 py-3 text-right">
                  <button (click)="deleteRule(rule.id!)" class="p-2 text-text-muted hover:text-red-500 hover:bg-red-50 rounded-lg transition-colors cursor-pointer" title="Supprimer cette règle">
                    <svg class="w-5 h-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="3 6 5 6 21 6"></polyline><path d="M19 6v14a2 2 0 0 1-2 2H7a2 2 0 0 1-2-2V6m3 0V4a2 2 0 0 1 2-2h4a2 2 0 0 1 2 2v2"></path></svg>
                  </button>
                </td>
              </tr>
            }
            @if (rules().length === 0) {
              <tr>
                <td colspan="3" class="px-5 py-10 text-center text-sm text-text-muted">La liste noire est actuellement vide.</td>
              </tr>
            }
          </tbody>
        </table>
      </div>
    </section>
  `
})
export class BlacklistComponent implements OnInit {
  private blacklistService = inject(BlacklistService);

  rules = signal<BlacklistRule[]>([]);
  newRuleType = signal<string>('Artist');
  newRuleValue = signal<string>('');

  ngOnInit() {
    this.loadRules();
  }

  loadRules() {
    this.blacklistService.getRules().subscribe(data => {
      this.rules.set(data);
    });
  }

  addRule() {
    const val = this.newRuleValue().trim();
    if (!val) return;

    this.blacklistService.addRule({ ruleType: this.newRuleType(), value: val }).subscribe(() => {
      this.newRuleValue.set('');
      this.loadRules(); // reload to get ID and ensure no duplicates
    });
  }

  deleteRule(id: number) {
    this.blacklistService.deleteRule(id).subscribe(() => {
      this.rules.set(this.rules().filter(r => r.id !== id));
    });
  }
}
