import { useEffect } from 'react';
import { userManager } from './authConfig';

export function SilentCallback() {
    useEffect(() => {
        userManager.signinSilentCallback()
            .catch((err) => {
                console.error('Silent renew callback failed:', err);
            });
    }, []);

    return (
        <div style={{ padding: '20px', color: '#a6adc8' }}>
            Processing renewal...
        </div>
    );
}
