import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { Project, ProjectCreateRequest, ProjectUpdateRequest, ProjectHealth, RiskyProject, ProjectLines } from '../models/project';

@Injectable({
  providedIn: 'root',
})

export class ProjectsService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = '/api/Projects';

  getProjects(): Observable<Project[]> {
    return this.http.get<Project[]>(this.apiUrl);
  }

  getProjectById(id: number): Observable<Project> {
    return this.http.get<Project>(`${this.apiUrl}/${id}`);
  }

  createProject(dto: ProjectCreateRequest): Observable<Project> {
    return this.http.post<Project>(this.apiUrl, dto);
  }

  updateProject(id: number, dto: ProjectUpdateRequest): Observable<Project> {
    return this.http.put<Project>(`${this.apiUrl}/${id}`, dto);
  }

  deleteProject(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  getProjectHealth(id: number): Observable<ProjectHealth> {
    return this.http.get<ProjectHealth>(`${this.apiUrl}/${id}/health`);
  }

  getRiskyProjects(): Observable<RiskyProject[]> {
    return this.http.get<RiskyProject[]>(`${this.apiUrl}/risky`);
  }

  getProjectLines(): Observable<ProjectLines[]> {
    return this.http.get<ProjectLines[]>(`${this.apiUrl}/lines`);
  }
}
