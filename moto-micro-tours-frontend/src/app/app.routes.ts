import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  {
    path: '',
    redirectTo: 'landing',
    pathMatch: 'full'
  },
  {
    path: 'home',
    redirectTo: 'landing',
    pathMatch: 'full'
  },
  {
    path: 'landing',
    loadComponent: () => import('./features/home/home.component').then(m => m.HomeComponent)
  },
  {
    path: 'auth',
    children: [
      {
        path: 'login',
        loadComponent: () => import('./features/auth/login/login.component').then(m => m.LoginComponent)
      },
      {
        path: 'register',
        loadComponent: () => import('./features/auth/register/register.component').then(m => m.RegisterComponent)
      }
    ]
  },
  {
    path: 'tours',
    children: [
      {
        path: '',
        loadComponent: () => import('./features/tours/tour-list/tour-list.component').then(m => m.TourListComponent)
      },
      {
        path: 'create',
        canActivate: [roleGuard('Motard')],
        loadComponent: () => import('./features/tours/tour-create/tour-create.component').then(m => m.TourCreateComponent)
      },
      {
        path: ':id',
        loadComponent: () => import('./features/tours/tour-detail/tour-detail.component').then(m => m.TourDetailComponent)
      }
    ]
  },
  {
    path: 'motards/:userId',
    loadComponent: () => import('./features/profile/motard-profile/motard-profile.component').then(m => m.MotardProfileComponent)
  },
  {
    path: 'messages',
    canActivate: [authGuard],
    loadComponent: () => import('./features/chats/inbox/prebooking-inbox.component').then(m => m.PrebookingInboxComponent)
  },
  {
    path: 'bookings',
    canActivate: [authGuard],
    loadChildren: () => import('./features/bookings/bookings.routes').then(m => m.bookingsRoutes)
  },
  {
    path: 'dashboard',
    canActivate: [authGuard],
    loadChildren: () => import('./features/dashboard/dashboard.routes').then(m => m.dashboardRoutes)
  },
  {
    path: 'motard',
    canActivate: [roleGuard('Motard')],
    children: [
      {
        path: 'dashboard',
        redirectTo: '/dashboard/motard',
        pathMatch: 'full'
      },
      {
        path: 'tours',
        loadComponent: () => import('./features/motard/tours/tours.component').then(m => m.ToursComponent)
      }
    ]
  },
  {
    path: 'participant',
    canActivate: [roleGuard('Participant')],
    children: [
      {
        path: 'dashboard',
        redirectTo: '/dashboard/participant',
        pathMatch: 'full'
      },
      {
        path: 'tours',
        loadComponent: () => import('./features/participant/tours/tours.component').then(m => m.ToursComponent)
      }
    ]
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    loadComponent: () => import('./features/profile/components/profile-redirect.component').then(m => m.ProfileRedirectComponent)
  },
  {
    path: 'unauthorized',
    loadComponent: () => import('./shared/components/unauthorized/unauthorized.component').then(m => m.UnauthorizedComponent)
  },
  {
    path: '**',
    redirectTo: 'landing'
  }
];
