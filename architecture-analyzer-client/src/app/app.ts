import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

import { Header } from '../app/layout/header/header'
import { Navigation } from '../app/layout/navigation/navigation';
import { Footer } from '../app/layout/footer/footer';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, Header, Navigation, Footer],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
}
