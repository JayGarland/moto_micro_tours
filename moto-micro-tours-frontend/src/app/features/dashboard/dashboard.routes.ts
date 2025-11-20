import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';

export const dashboardRoutes: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./role-aware-dashboard/role-aware-dashboard.component').then(
        (m) => m.RoleAwareDashboardComponent
      ),
  },
  {
    path: 'participant',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./participant-dashboard/participant-dashboard.component').then(
        (m) => m.ParticipantDashboardComponent
      ),
  },
  {
    path: 'motard',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./motard-dashboard/motard-dashboard.component').then(
        (m) => m.MotardDashboardComponent
      ),
  },
];
