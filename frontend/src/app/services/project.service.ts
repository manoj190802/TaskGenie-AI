import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Project } from '../models/models';

@Injectable({ providedIn: 'root' })
export class ProjectService {
  private apiUrl = 'http://localhost:5000/api/projects';
  constructor(private http: HttpClient) {}

  getAll(status?: string): Observable<any[]> {
    let params = new HttpParams();
    if (status) params = params.set('status', status);
    return this.http.get<any[]>(this.apiUrl, { params });
  }

  getById(id: string): Observable<any> {
    return this.http.get<any>(`${this.apiUrl}/${id}`);
  }

  create(project: Partial<Project>): Observable<Project> {
    return this.http.post<Project>(this.apiUrl, project);
  }

  /**
   * Full wizard flow: Create project + upload file + AI analysis in one request.
   * Returns project, extracted tasks with AI recommendations, tech stack, etc.
   */
  createWithUpload(formData: FormData): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/create-with-upload`, formData);
  }

  update(id: string, project: Partial<Project>): Observable<Project> {
    return this.http.put<Project>(`${this.apiUrl}/${id}`, project);
  }

  uploadRequirements(id: string, file: File): Observable<any> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<any>(`${this.apiUrl}/${id}/upload-requirements`, formData);
  }

  analyzeRequirements(id: string): Observable<any> {
    return this.http.post<any>(`${this.apiUrl}/${id}/analyze`, {});
  }

  delete(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }
}
