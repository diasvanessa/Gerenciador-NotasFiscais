import { Routes } from '@angular/router';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { ProdutosComponent } from './components/produtos/produtos.component';
import { FaturamentoComponent } from './components/faturamento/faturamento.component';

export const routes: Routes = [
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
  { path: 'dashboard', component: DashboardComponent },
  { path: 'produtos', component: ProdutosComponent },
  { path: 'faturamento', component: FaturamentoComponent },
  { path: '**', redirectTo: 'dashboard' }
];
