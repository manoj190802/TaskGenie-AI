import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { DashboardStats } from '../models/models';

@Injectable({ providedIn: 'root' })
export class ReportService {
  private apiUrl = 'http://localhost:5000/api/reports';
  constructor(private http: HttpClient) {}

  getDashboardStats(): Observable<DashboardStats> {
    return this.http.get<DashboardStats>(`${this.apiUrl}/dashboard`);
  }

  getDeveloperWorkload(): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/developer-workload`);
  }

  getTaskSummary(projectId?: string): Observable<any> {
    const url = projectId
      ? `${this.apiUrl}/task-summary?projectId=${projectId}`
      : `${this.apiUrl}/task-summary`;
    return this.http.get<any>(url);
  }

  getAssignmentHistory(page = 1, pageSize = 20): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/assignment-history?page=${page}&pageSize=${pageSize}`);
  }

  getAiStats(): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/ai-stats`);
  }
}
