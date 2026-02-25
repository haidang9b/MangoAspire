import axios from 'axios';
import type { UserInfo } from '../types';

const IDENTITY_URL = import.meta.env.VITE_IDENTITY_URL ?? 'https://localhost:8080';

export const userApi = {
    async getUserInfo(accessToken: string): Promise<UserInfo> {
        const response = await axios.get<UserInfo>(`${IDENTITY_URL}/connect/userinfo`, {
            headers: {
                Authorization: `Bearer ${accessToken}`,
            },
        });
        return response.data;
    },
};
