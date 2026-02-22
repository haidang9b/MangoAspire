import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: parseInt(process.env.PORT ?? '5173'),
    strictPort: true,
    proxy: {
      // Forward all /api/* requests to the YARP gateway.
      // GATEWAY_URL is injected by Aspire at startup.
      '/api': {
        target: process.env.GATEWAY_URL ?? 'http://localhost:5000',
        changeOrigin: true,
        secure: false,
      },
    },
  },
})
