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

        instance.interceptors.request.use(async (config) => {
            const user = await userManager.getUser();
            if (user?.access_token) {
                config.headers.Authorization = `Bearer ${user.access_token}`;
            }
            return config;
        });

        return instance;
    }, []);

    return apiClient;
}
