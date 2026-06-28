import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Developer } from '../models/models';

@Injectable({ providedIn: 'root' })
export class DeveloperService {
  private apiUrl = 'http://localhost:5000/api/developers';
  constructor(private http: HttpClient) {}

  getAll(availability?: string, skill?: string): Observable<Developer[]> {
    let params = new HttpParams();
    if (availability) params = params.set('availability', availability);
    if (skill) params = params.set('skill', skill);
    return this.http.get<Developer[]>(this.apiUrl, { params });
  }

  getById(id: string): Observable<Developer> {
    return this.http.get<Developer>(`${this.apiUrl}/${id}`);
  }

  create(dev: Partial<Developer>): Observable<Developer> {
    return this.http.post<Developer>(this.apiUrl, dev);
  }

  update(id: string, dev: Partial<Developer>): Observable<Developer> {
    return this.http.put<Developer>(`${this.apiUrl}/${id}`, dev);
  }

  updateAvailability(id: string, availability: string, currentWorkload?: number): Observable<any> {
    return this.http.patch(`${this.apiUrl}/${id}/availability`, { availability, currentWorkload });
  }

  delete(id: string): Observable<any> {
    return this.http.delete(`${this.apiUrl}/${id}`);
  }

  getAssignments(id: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.apiUrl}/${id}/assignments`);
  }
}
