import { Component } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="app">
      <header>
        <h1>📦 Order & Inventory</h1>
        <nav>
          <a routerLink="/orders" routerLinkActive="active">Orders</a>
          <a routerLink="/inventory" routerLinkActive="active">Inventory</a>
        </nav>
      </header>
      <main>
        <router-outlet></router-outlet>
      </main>
    </div>
  `,
  styles: [`
    .app { font-family: system-ui, sans-serif; max-width: 900px; margin: 0 auto; }
    header { display: flex; justify-content: space-between; align-items: center; padding: 1rem; border-bottom: 1px solid #ddd; }
    h1 { margin: 0; font-size: 1.5rem; }
    nav { display: flex; gap: 1rem; }
    nav a { text-decoration: none; color: #333; padding: 0.5rem 1rem; border-radius: 4px; }
    nav a.active { background: #007bff; color: white; }
    main { padding: 1rem; }
  `]
})
export class App {}
