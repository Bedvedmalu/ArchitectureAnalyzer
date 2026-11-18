import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router'

import { Project } from '../../models/project';
import { ProjectsService } from '../../services/projects';
import { AuthService } from '../../services/auth';

type ViewMode = 'table' | 'cards';

type SortField = 'id' | 'projectName' | 'projectLanguage' | 'uploadDate';

type SortDirection = 'asc' | 'desc';

@Component({
  selector: 'app-projects',
  imports: [DatePipe, RouterLink],
  templateUrl: './projects.html',
  styleUrl: './projects.css'
})

export class Projects implements OnInit {
  private readonly projectsService = inject(ProjectsService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly title = 'Список проектов';

  readonly projects = signal<Project[]>([]);
  readonly isLoading = signal(false);
  readonly hasLoaded = signal(false);
  readonly showProjects = signal(true);
  readonly errorMessage = signal('');

  readonly viewMode = signal<ViewMode>('table');

  readonly searchQuery = signal('');
  readonly selectedLanguage = signal('all');

  readonly sortField = signal<SortField>('id');
  readonly sortDirection = signal<SortDirection>('asc');

  readonly isAdmin = computed(() => this.authService.role() === 'Admin');

  readonly deletingId = signal<number | null>(null);
  readonly actionError = signal('');

  readonly languages = computed(() => {
    const languages = this.projects().map(
      project => project.projectLanguage || 'Не указан'
    );

    return [...new Set(languages)].sort();
  });

  readonly visibleProjects = computed(() => {
    const query = this.searchQuery().trim().toLowerCase();
    const language = this.selectedLanguage();

    const filtered = this.projects().filter(project => {
      const nameMatches = project.projectName.toLowerCase().includes(query) || String(project.id).includes(query);

      const projectLanguage = project.projectLanguage || 'Не указан';

      const languageMatches = language === 'all' || projectLanguage === language;

      return nameMatches && languageMatches;
    });

    const field = this.sortField();
    const direction = this.sortDirection() === 'asc' ? 1 : -1;

    return [...filtered].sort((a, b) => {
      let result = 0;
      switch (field) {

        case 'id':
          result = a.id - b.id;
          break;

        case 'projectName':
          result = a.projectName.localeCompare(b.projectName, 'ru');
          break

        case 'projectLanguage':
          result = (a.projectLanguage || '').localeCompare(b.projectLanguage || '', 'ru');
          break;

        case 'uploadDate':
          result = Date.parse(a.uploadDate) - Date.parse(b.uploadDate);
          break;
      }

      return result * direction;
    });
  });

  ngOnInit(): void {
    this.loadProjects();
  }

  loadProjects(): void {
    this.isLoading.set(true);
    this.hasLoaded.set(false);
    this.errorMessage.set('');

    this.projectsService.getProjects().subscribe({
      next: (data: Project[]) => {
        this.projects.set(data);
        this.isLoading.set(false);
        this.hasLoaded.set(true);
      },
      error: (error: HttpErrorResponse) => {
        this.isLoading.set(false);

        if (error.status === 401) {
          this.authService.logout();
          void this.router.navigateByUrl('/login');
          return;
        } else if (error.status === 403) {
          this.errorMessage.set('Your role is not Admin');
        } else if (error.status === 0) {
          this.errorMessage.set('Cannot connect');
        } else {
          this.errorMessage.set(`Error with code: ${error.status}`);
        }
      }
    });
  }

  deleteProject(project: Project): void {
    if (!this.isAdmin() || this.deletingId() !== null) {
      return;
    }

    const confirmed = window.confirm(`Удалить проет "${project.projectName}"?`);
    if (!confirmed) return;

    this.deletingId.set(project.id);
    this.actionError.set('');

    this.projectsService.deleteProject(project.id).subscribe({
      next: () => {
        this.projects.update(items => items.filter(item => item.id !== project.id));

        this.deletingId.set(null);
      },

      error: (error: HttpErrorResponse) => {
        this.deletingId.set(null);
        if (error.status === 401) {
          this.authService.logout();
          void this.router.navigateByUrl('/login');
          return;
        } else if (error.status === 403) {
          this.actionError.set('Недостаточно прав');
        } else if (error.status === 404) {
          this.actionError.set('Проект не найден');
        } else {
          this.actionError.set(`Ошибка удаления с кодом: ${error.status}`);
        }
      }
    });
  }

  onSearchInput(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.searchQuery.set(input.value);
  }

  onLanguageChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.selectedLanguage.set(select.value);
  }

  onSortChange(event: Event): void {
    const select = event.target as HTMLSelectElement;
    this.sortField.set(select.value as SortField);
  }

  toggleSortDirection(): void {
    this.sortDirection.update(
      direction => direction === 'asc' ? 'desc' : 'asc'
    );
  }

  sortBy(field: SortField): void {
    if (this.sortField() === field) {
      this.toggleSortDirection();
    } else {
      this.sortField.set(field);
      this.sortDirection.set('asc');
    }
  }

  sortIndicator(field: SortField): string {
    if (this.sortField() !== field) {
      return '';
    }

    return this.sortDirection() === 'asc' ? 'Up' : 'Down';
  }

  clearFilters(): void {
    this.searchQuery.set('');
    this.selectedLanguage.set('all');
  }
}
