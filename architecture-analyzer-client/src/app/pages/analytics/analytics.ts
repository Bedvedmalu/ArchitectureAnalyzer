import { Component, computed, inject, OnInit, signal } from '@angular/core';

import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { RiskyProject, ProjectLines } from '../../models/project';

import { ProjectsService } from '../../services/projects';

@Component({
  selector: 'app-analytics',
  imports: [DecimalPipe, RouterLink],
  templateUrl: './analytics.html',
  styleUrl: './analytics.css',
})

export class Analytics implements OnInit {
  private readonly projectsService = inject(ProjectsService);

  readonly riskyProjects = signal<RiskyProject[]>([]);
  readonly linesStats = signal<ProjectLines[]>([]);

  readonly isLoading = signal(false);
  readonly hasLoaded = signal(false);
  readonly errorMessage = signal('');

  readonly totalRiskyClasses = computed(() => this.riskyProjects().reduce((sum, project) => sum + project.riskyClassesCount, 0));

  readonly totalLines = computed(() => this.linesStats().reduce((sum, project) => sum + project.totalLines, 0));

  ngOnInit(): void {
    this.loadAnalytics();
  }

  loadAnalytics(): void {
    this.isLoading.set(true);
    this.hasLoaded.set(false);
    this.errorMessage.set('');

    forkJoin({
      risky: this.projectsService.getRiskyProjects(),
      lines: this.projectsService.getProjectLines()
    }).subscribe({
      next: (result) => {
        this.riskyProjects.set(result.risky);
        this.linesStats.set(result.lines);

        this.hasLoaded.set(true);
        this.isLoading.set(false);
      },

      error: (error: HttpErrorResponse) => {
        this.isLoading.set(false);

        this.errorMessage.set(`Не удалось загрузить аналитику с кодом: ${error.status}`);
      }
    });
  }
}
