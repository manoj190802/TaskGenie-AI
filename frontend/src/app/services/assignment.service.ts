import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Assignment } from '../models/models';

@Injectable({ providedIn: 'root' })
export class AssignmentService {
  private apiUrl = 'http://localhost:5000/api/assignments';
  constructor(private http: HttpClient) {}

  getAll(developerId?: string, projectId?: string, status?: string): Observable<Assignment[]> {
    let params = new HttpParams();
    if (developerId) params = params.set('developerId', developerId);
    if (projectId) params = params.set('projectId', projectId);
    if (status) params = params.set('status', status);
    return this.http.get<Assignment[]>(this.apiUrl, { params });
  }

  getById(id: string): Observable<Assignment> {
    return this.http.get<Assignment>(`${this.apiUrl}/${id}`);
  }

  create(payload: any): Observable<Assignment> {
    return this.http.post<Assignment>(this.apiUrl, payload);
  }

  reassign(id: string, newDeveloperId: string, reason: string): Observable<any> {
    return this.http.post(`${this.apiUrl}/${id}/reassign`, { newDeveloperId, reason });
  }

  updateStatus(id: string, status: string): Observable<any> {
    return this.http.patch(`${this.apiUrl}/${id}/status`, { status });
  }
}
