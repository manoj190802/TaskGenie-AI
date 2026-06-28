import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ReportService {
  private apiUrl = 'http://localhost:5000/api/reports';
  constructor(private http: HttpClient) {}

  getDashboardStats(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/dashboard`);
  }

  getDeveloperWorkload(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/developer-workload`);
  }

  getProjectsSummary(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/projects-summary`);
  }
}
