import { Routes } from '@angular/router';
import { ExplorerPage } from './features/explorer/explorer-page';

export const routes: Routes = [
  { path: '', component: ExplorerPage, title: 'Vehicle Explorer' },
  { path: '**', redirectTo: '' },
];
