import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

import path from 'path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
    // Force a single React instance — prevents "Invalid hook call" errors
    // that occur when pnpm symlinks cause packages to resolve React from
    // different node_modules paths (e.g. @tanstack/react-query-devtools).
    dedupe: ['react', 'react-dom'],
  },
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

