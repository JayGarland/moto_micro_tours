// User authentication models matching backend DTOs

export interface CurrentUser {
  userId: string;
  username: string;
  email: string | null;
  roles: string[];
}

export interface LoginRequest {
  emailOrUsername: string;
  password: string;
}

export interface LoginResponse {
  userId: string;
  username: string;
  email: string | null;
  roles: string[];
}
