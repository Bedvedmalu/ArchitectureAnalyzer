import { Component, inject, OnInit, signal } from '@angular/core';

import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { Project, ProjectHealth } from '../../models/project';

import { ProjectsService } from '../../services/projects';

@Component({
  selector: 'app-project-details',
  imports: [DatePipe, RouterLink],
  templateUrl: './project-details.html',
  styleUrl: './project-details.css'
})

export class ProjectDetails implements OnInit {
  private readonly projectsService = inject(ProjectsService);
  private readonly route = inject(ActivatedRoute);

  readonly project = signal<Project | null>(null);
  readonly health = signal<ProjectHealth | null>(null);

  readonly isLoading = signal(false);
  readonly errorMessage = signal('');

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');
    const id = Number(idParam);

    if (idParam === null || !Number.isSafeInteger(id) || id <= 0) {
      this.errorMessage.set('Некорректный ID проекта');
      return;
    }
    this.loadProject(id);
  }

  private loadProject(id: number): void {
    this.isLoading.set(true);
    this.errorMessage.set('');

    forkJoin({
      project: this.projectsService.getProjectById(id),
      health: this.projectsService.getProjectHealth(id)
    }).subscribe({
      next: (result) => {
        this.project.set(result.project);
        this.health.set(result.health);
        this.isLoading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.isLoading.set(false);

        if (error.status === 404) {
          this.errorMessage.set('Проект не найден');
        } else if (error.status === 401) {
          this.errorMessage.set('Не авторизован');
        } else {
          this.errorMessage.set(`Ошибка с кодом: ${error.status}`);
        }

      }
    });
  }
}
