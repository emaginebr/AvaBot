export interface AuthCredentials {
  email: string;
  password: string;
}

export interface RegisterInfo {
  name: string;
  email: string;
  password: string;
}

export interface UserInfo {
  userId: number;
  name: string;
  email: string;
  createdAt: string;
}

export interface AuthResultInfo {
  token: string;
  expiresAt: string;
  user: UserInfo;
}

export interface PasswordChangeInfo {
  currentPassword: string;
  newPassword: string;
}
