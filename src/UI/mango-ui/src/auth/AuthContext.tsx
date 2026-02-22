import {
    createContext,
    useCallback,
    useContext,
    useEffect,
    useState,
    type ReactNode,
} from 'react';
import type { User } from 'oidc-client-ts';
import { userManager } from './authConfig';

interface AuthContextValue {
    user: User | null;
    isLoading: boolean;
    login: () => Promise<void>;
    logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
    const [user, setUser] = useState<User | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    useEffect(() => {
        // Load existing session from storage
        userManager.getUser().then((u) => {
            setUser(u);
            setIsLoading(false);
        });

        // Keep state in sync with token renewals / expirations
        const onUserLoaded = (u: User) => setUser(u);
        const onUserUnloaded = () => setUser(null);

        userManager.events.addUserLoaded(onUserLoaded);
        userManager.events.addUserUnloaded(onUserUnloaded);
        userManager.events.addSilentRenewError(() => setUser(null));

        return () => {
            userManager.events.removeUserLoaded(onUserLoaded);
            userManager.events.removeUserUnloaded(onUserUnloaded);
        };
    }, []);

    const login = useCallback(() => userManager.signinRedirect(), []);
    const logout = useCallback(
        () => userManager.signoutRedirect(),
        []
    );

    return (
        <AuthContext.Provider value={{ user, isLoading, login, logout }}>
            {children}
        </AuthContext.Provider>
    );
}

export function useAuth(): AuthContextValue {
    const ctx = useContext(AuthContext);
    if (!ctx) throw new Error('useAuth must be used inside <AuthProvider>');
    return ctx;
}
