import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { TaskItem, MatchResult } from '../models/models';

@Injectable({ providedIn: 'root' })
export class TaskService {
  private apiUrl = 'http://localhost:5000/api/tasks';
  constructor(private http: HttpClient) {}

  getAll(projectId?: string, status?: string, category?: string): Observable<TaskItem[]> {
    let params = new HttpParams();
    if (projectId) params = params.set('projectId', projectId);
    if (status) params = params.set('status', status);
    if (category) params = params.set('category', category);
    return this.http.get<TaskItem[]>(this.apiUrl, { params });
  }

  getById(id: string): Observable<TaskItem> {
    return this.http.get<TaskItem>(`${this.apiUrl}/${id}`);
  }

  create(task: Partial<TaskItem>): Observable<TaskItem> {
    return this.http.post<TaskItem>(this.apiUrl, task);
  }

  updateStatus(id: string, status: string): Observable<any> {
    return this.http.patch(`${this.apiUrl}/${id}/status`, { status });
  }

  getRecommendations(id: string): Observable<MatchResult> {
    return this.http.post<MatchResult>(`${this.apiUrl}/${id}/recommend`, {});
  }

  delete(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
