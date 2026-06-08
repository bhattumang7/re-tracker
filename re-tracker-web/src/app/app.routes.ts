import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  {
    path: 'dashboard',
    loadComponent: () => import('./dashboard/dashboard.component').then(m => m.DashboardComponent)
  },
  {
    path: 'milestones',
    loadComponent: () => import('./milestones/milestone-list.component').then(m => m.MilestoneListComponent)
  },
  {
    path: 'milestones/:id',
    loadComponent: () => import('./milestones/milestone-detail.component').then(m => m.MilestoneDetailComponent)
  },
  {
    path: 'methods',
    loadComponent: () => import('./methods/method-list.component').then(m => m.MethodListComponent)
  },
  {
    path: 'methods/:id',
    loadComponent: () => import('./methods/method-panel.component').then(m => m.MethodPanelComponent)
  },
  {
    path: 'files',
    loadComponent: () => import('./files/file-list.component').then(m => m.FileListComponent)
  },
  {
    path: 'search',
    loadComponent: () => import('./search/search.component').then(m => m.SearchComponent)
  }
];
