import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  role: 'Admin' | 'User';
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly http = inject(HttpClient);

  private readonly tokenState = signal<string | null>(
    sessionStorage.getItem('auth_token')
  );

  private readonly roleState = signal<string | null>(
    sessionStorage.getItem('auth_role')
  );

  readonly isLoggedIn = computed(() => this.tokenState() !== null);
  readonly role = this.roleState.asReadonly();

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(
      '/api/Auth/login',
      credentials
    ).pipe(
      tap(response => {
        sessionStorage.setItem('auth_token', response.token);
        sessionStorage.setItem('auth_role', response.role);

        this.tokenState.set(response.token);
        this.roleState.set(response.role);
      })
    );
  }

  getToken(): string | null {
    const token = this.tokenState();

    if (!token) {
      return null;
    }

    if (this.isTokenExpired(token)) {
      this.logout();
      return null;
    }

    return token;
  }

  isAuthenticated(): boolean {
    return this.getToken() !== null;
  }

  logout(): void {
    sessionStorage.removeItem('auth_token');
    sessionStorage.removeItem('auth_role');

    this.tokenState.set(null);
    this.roleState.set(null);
  }

  private isTokenExpired(token: string): boolean {
    try {
      const payloadPart = token.split('.')[1];

      const payload = JSON.parse(
        atob(
          payloadPart.replace(/-/g, '+').replace(/_/g, '/')
        )
      ) as { exp?: number };

      return typeof payload.exp !== 'number'
        || payload.exp * 1000 <= Date.now();

    } catch {
      return true;
    }
  }
}
