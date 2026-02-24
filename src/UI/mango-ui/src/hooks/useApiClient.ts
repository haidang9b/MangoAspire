import axios, { type AxiosInstance } from 'axios';
import { useMemo } from 'react';
import { userManager } from '../auth/authConfig';

const BASE_URL = import.meta.env.VITE_GATEWAY_URL ?? 'https://localhost:7000';

export function useApiClient(): AxiosInstance {
    const apiClient = useMemo(() => {
        const instance = axios.create({
            baseURL: BASE_URL,
            headers: { 'Content-Type': 'application/json' },
        });

        // ── Request interceptor: attach Bearer token ─────────────────────────
        instance.interceptors.request.use(async (config) => {
            const user = await userManager.getUser();
            if (user?.access_token) {
                config.headers.Authorization = `Bearer ${user.access_token}`;
            }
            return config;
        });

        // ── Response interceptor: handle 401 (token expired) ─────────────────
        instance.interceptors.response.use(
            (response) => response,
            async (error) => {
                const originalRequest = error.config;

                // Only handle 401s once; skip auth/callback endpoints
                if (
                    error.response?.status !== 401 ||
                    originalRequest._retry
                ) {
                    return Promise.reject(error);
                }

                originalRequest._retry = true;

                try {
                    // Attempt silent token renewal
                    const renewedUser = await userManager.signinSilent();
                    if (renewedUser?.access_token) {
                        originalRequest.headers.Authorization = `Bearer ${renewedUser.access_token}`;
                        return instance(originalRequest);
                    }
                } catch {
                    // Silent renew failed — session is fully expired
                }

                // Fall back to full login redirect
                await userManager.signinRedirect();
                return Promise.reject(error);
            }
        );

        return instance;
    }, []);

    return apiClient;
}

