export interface AuthCredentials {
  username: string;
  password: string;
}

export interface AuthResponse {
  sucesso: boolean;
  token: string;
}
