import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class AuthService {

  constructor(private http: HttpClient) {}

  registerStart(username: string) {
    return this.http.post<any>(
      '/api/auth/RegisterStart',
      username
    );
  }

  registerFinish(data: any) {
    return this.http.post(
      '/api/auth/RegisterFinish',
      data
    );
  }
}