import { Routes } from '@angular/router';


import { Login } from './pages/login/login';
import { Projects } from './pages/projects/projects';
import { ProjectForm } from './pages/project-form/project-form';
import { ProjectDetails } from './pages/project-details/project-details';
import { Analytics } from './pages/analytics/analytics';

import { authGuard } from './guards/auth-guard';
import { adminGuard } from './guards/admin-guard';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'projects'
  },
  {
    path: 'login',
    component: Login
  },
  {
    path: 'projects/new',
    component: ProjectForm,
    canActivate: [authGuard, adminGuard]
  },
  {
    path: 'projects/:id/edit',
    component: ProjectForm,
    canActivate: [authGuard, adminGuard]
  },
  {
    path: 'projects/:id',
    component: ProjectDetails,
    canActivate: [authGuard]
  },
  {
    path: 'projects',
    component: Projects,
    canActivate: [authGuard]
  },
  {
    path: 'analytics',
    component: Analytics,
    canActivate: [authGuard]
  },
  {
    path: '**',
    redirectTo: 'projects'
  }
];
