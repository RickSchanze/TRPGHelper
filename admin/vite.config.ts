import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// 管理台跑在 5174，接口统一走 /api 代理到 5080，避免额外的 CORS 配置。
export default defineConfig({
  plugins: [vue()],
  server: {
    port: 5174,
    proxy: {
      '/api': {
        target: 'http://localhost:5080',
        changeOrigin: true,
      },
    },
  },
})
