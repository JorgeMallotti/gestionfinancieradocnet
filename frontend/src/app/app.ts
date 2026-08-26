import { Component, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { LanguageService } from './core/services/language.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  constructor() {
    // Initializes @ngx-translate with the stored/browser language.
    inject(LanguageService).init();
  }
}
