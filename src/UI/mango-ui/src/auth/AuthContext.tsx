import {
    useCallback,
    useEffect,
    useState,
    type ReactNode,
} from 'react';
import type { User } from 'oidc-client-ts';
import { userManager } from './authConfig';
import { userApi } from '../api/userApi';
import { AuthContext } from './AuthContextObject';
import type { UserInfo } from '../types';

export function AuthProvider({ children }: { children: ReactNode }) {
    const [user, setUser] = useState<User | null>(null);
    const [userInfo, setUserInfo] = useState<UserInfo | null>(null);
    const [isLoading, setIsLoading] = useState(true);

    const fetchUserInfo = useCallback(async (u: User) => {
        try {
            const info = await userApi.getUserInfo(u.access_token);
            setUserInfo(info);
        } catch (error) {
            console.error('Failed to fetch user info:', error);
            setUserInfo(null);
        }
    }, []);

    useEffect(() => {
        // Load existing session from storage
        const init = async () => {
            try {
                const u = await userManager.getUser();
                setUser(u);
                if (u) {
                    await fetchUserInfo(u);
                }
            } finally {
                setIsLoading(false);
            }
        };

        init();

        // Keep state in sync with token renewals / expirations
        const onUserLoaded = (u: User) => {
            setUser(u);
            fetchUserInfo(u);
        };
        const onUserUnloaded = () => {
            setUser(null);
            setUserInfo(null);
        };

        userManager.events.addUserLoaded(onUserLoaded);
        userManager.events.addUserUnloaded(onUserUnloaded);
        userManager.events.addSilentRenewError(() => {
            setUser(null);
            setUserInfo(null);
        });

        return () => {
            userManager.events.removeUserLoaded(onUserLoaded);
            userManager.events.removeUserUnloaded(onUserUnloaded);
        };
    }, [fetchUserInfo]);

    const login = useCallback(() => userManager.signinRedirect(), []);
    const logout = useCallback(
        () => userManager.signoutRedirect(),
        []
    );

    return (
        <AuthContext.Provider value={{ user, userInfo, isLoading, login, logout }}>
            {children}
        </AuthContext.Provider>
    );
}
