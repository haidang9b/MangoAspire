import { createContext } from 'react';
import type { User } from 'oidc-client-ts';
import type { UserInfo } from '../types';

export interface AuthContextValue {
    user: User | null;
    userInfo: UserInfo | null;
    isLoading: boolean;
    login: () => Promise<void>;
    logout: () => Promise<void>;
}

export const AuthContext = createContext<AuthContextValue | null>(null);
