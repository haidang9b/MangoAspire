import { UserManager, WebStorageStateStore } from 'oidc-client-ts';

const IDENTITY_URL = import.meta.env.VITE_IDENTITY_URL ?? 'https://localhost:8080'; // identity server

export const userManager = new UserManager({
    authority: IDENTITY_URL,
    client_id: 'mango-spa',
    redirect_uri: `${window.location.origin}/callback`,
    silent_redirect_uri: `${window.location.origin}/silent-callback`,
    post_logout_redirect_uri: window.location.origin,
    response_type: 'code',
    scope: 'openid profile email mango offline_access',
    userStore: new WebStorageStateStore({ store: window.localStorage }),
    automaticSilentRenew: true,
    loadUserInfo: true,
});
