import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface BlacklistRule {
  id?: number;
  ruleType: string;
  value: string;
}

@Injectable({
  providedIn: 'root'
})
export class BlacklistService {
  private http = inject(HttpClient);
  private apiUrl = 'http://localhost:5001/api/blacklist';

  getRules(): Observable<BlacklistRule[]> {
    return this.http.get<BlacklistRule[]>(this.apiUrl);
  }

  addRule(rule: BlacklistRule): Observable<BlacklistRule> {
    return this.http.post<BlacklistRule>(this.apiUrl, rule);
  }

  deleteRule(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
