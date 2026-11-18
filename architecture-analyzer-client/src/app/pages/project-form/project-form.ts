import { Component, inject, OnInit, signal } from '@angular/core';
import { ReactiveFormsModule, FormControl, FormGroup, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { Observable } from 'rxjs';

import { Project } from '../../models/project';
import { ProjectsService } from '../../services/projects';

function httpUrlValidator(control: AbstractControl): ValidationErrors | null {
  const value = String(control.value ?? '').trim();

  if (!value) return null;

  try {
    const url = new URL(value);

    if ((url.protocol === 'http:' || url.protocol === 'https:') && url.hostname.includes('.')) {
      return null;
    }
  } catch {

  }

  return { invalidUrl: true };
}

@Component({
  selector: 'app-project-form',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './project-form.html',
  styleUrl: './project-form.css'
})

export class ProjectForm implements OnInit {
  private readonly projectsService = inject(ProjectsService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  readonly isEdit = signal(false);
  readonly isLoading = signal(false);
  readonly isSaving = signal(false);
  readonly loadError = signal('');
  readonly errorMessage = signal('');

  private projectId: number | null = null;

  readonly projectForm = new FormGroup({
    projectName: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.maxLength(150)
      ]
    }),

    projectUrl: new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        httpUrlValidator
      ]
    })
  });

  ngOnInit(): void {
    const idParam = this.route.snapshot.paramMap.get('id');

    if (idParam === null) {
      return;
    }

    this.isEdit.set(true);

    this.projectForm.controls.projectUrl.disable();

    const id = Number(idParam);

    if (!Number.isSafeInteger(id) || id <= 0) {
      this.loadError.set('Некорректный ID проекта');
      return;
    }

    this.projectId = id;
    this.loadProject(id);
  }

  private loadProject(id: number): void {
    this.isLoading.set(true);

    this.projectsService.getProjectById(id).subscribe({
      next: (project: Project) => {
        this.projectForm.patchValue({
          projectName: project.projectName
        });

        this.isLoading.set(false);
      },

      error: (error: HttpErrorResponse) => {
        this.isLoading.set(false);
        this.loadError.set(error.status === 404 ? 'Проект не найден' : `Не удалось загрузить проект. Код ${error.status}`);
      } 
    });
  }

  onSubmit(): void {
    if (this.isSaving()) return;

    if (this.projectForm.invalid) {
      this.projectForm.markAllAsTouched();
      return;
    }

    const projectName = this.projectForm.controls.projectName.value.trim();
    const projectUrl = this.projectForm.controls.projectUrl.value.trim();

    if (!projectName) {
      this.projectForm.controls.projectName.setErrors({
        required: true
      });
      return;
    }

    let requests: Observable<Project>;
    if (this.isEdit()) {
      if (this.projectId === null) return;

      requests = this.projectsService.updateProject(
        this.projectId,
        {
          projectName
        }
      );
    } else {
      requests = this.projectsService.createProject({
        projectName,
        projectUrl
      });
    }

    this.isSaving.set(true);
    this.errorMessage.set('');

    requests.subscribe({
      next: () => {
        this.isSaving.set(false);
        void this.router.navigateByUrl('/projects');
      },

      error: (error: HttpErrorResponse) => {
        this.isSaving.set(false);

        if (error.status === 400) {
          this.errorMessage.set('Проверьте поля формы');
        } else if (error.status === 403) {
          this.errorMessage.set('Недостаточно прав');
        } else if (error.status === 404) {
          this.errorMessage.set('Проет не найден');
        } else if (error.status === 0) {
          this.errorMessage.set('Нет соединения');
        } else {
          this.errorMessage.set(`Ошибка с кодом: ${error.status}`);
        }
      }
    });
  }   
}


