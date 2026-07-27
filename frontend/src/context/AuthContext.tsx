import React, { createContext, useContext, useState } from 'react';
import api from '../api/axiosInstance';
import type { User, AuthResponse, ApiResponse } from '../types';

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  loading: boolean;
  login: (credentials: { email: string; password: string }) => Promise<void>;
  register: (data: {
    name: string;
    email: string;
    password: string;
    businessName: string;
    gstin?: string;
    state: string;
    address?: string;
  }) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(() => {
    const saved = localStorage.getItem('user');
    return saved ? JSON.parse(saved) : null;
  });
  const [loading, setLoading] = useState(false);

  const saveAuth = (auth: AuthResponse) => {
    localStorage.setItem('accessToken', auth.accessToken);
    localStorage.setItem('refreshToken', auth.refreshToken);
    localStorage.setItem('user', JSON.stringify(auth.user));
    setUser(auth.user);
  };

  const login = async (credentials: { email: string; password: string }) => {
    setLoading(true);
    try {
      const res = await api.post<ApiResponse<AuthResponse>>('/auth/login', credentials);
      if (res.data.success && res.data.data) {
        saveAuth(res.data.data);
      } else {
        throw new Error(res.data.error || 'Login failed');
      }
    } finally {
      setLoading(false);
    }
  };

  const register = async (data: any) => {
    setLoading(true);
    try {
      const res = await api.post<ApiResponse<AuthResponse>>('/auth/register', data);
      if (res.data.success && res.data.data) {
        saveAuth(res.data.data);
      } else {
        throw new Error(res.data.error || 'Registration failed');
      }
    } finally {
      setLoading(false);
    }
  };

  const logout = () => {
    localStorage.clear();
    setUser(null);
    window.location.href = '/login';
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        loading,
        login,
        register,
        logout,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
