export interface CurrentUser {
  id: string;
  userName: string;
  email: string;
  roles?: string[];
  loginTime?: Date;
}

export interface AuthResponse {
  userId: string;
  userName: string;
  email: string;
  token: string;
  roles?: string[];
}

export interface LoginModel {
  email: string;
  password: string;
}

export interface RegisterModel {
  userName?: string;
  email: string;
  password: string;
  confirmPassword?: string;
  role?: string; // Added role field
}