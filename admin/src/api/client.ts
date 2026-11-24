import axios from 'axios'
import type { AdminUser } from './types'

const tokenKey = 'trpg_admin_token'
const userKey = 'trpg_admin_user'

export const http = axios.create({
  baseURL: import.meta.env.VITE_API_BASE || '',
  timeout: 20000,
})

http.interceptors.request.use((config) => {
  const token = localStorage.getItem(tokenKey)
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

http.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error?.response?.status === 401) {
      clearSession()
      if (!window.location.hash.startsWith('#/login')) {
        window.location.hash = '#/login'
      }
    }
    return Promise.reject(error)
  },
)

export function getToken(): string | null {
  return localStorage.getItem(tokenKey)
}

export function getStoredUser(): AdminUser | null {
  const raw = localStorage.getItem(userKey)
  if (!raw) {
    return null
  }
  try {
    return JSON.parse(raw) as AdminUser
  } catch {
    return null
  }
}

export function setSession(token: string, user: AdminUser): void {
  localStorage.setItem(tokenKey, token)
  localStorage.setItem(userKey, JSON.stringify(user))
}

export function clearSession(): void {
  localStorage.removeItem(tokenKey)
  localStorage.removeItem(userKey)
}

export function errorMessage(error: unknown): string {
  const response = (error as { response?: { data?: unknown; status?: number } })?.response
  const data = response?.data as
    | { title?: string; detail?: string; message?: string; errors?: Record<string, string[]> }
    | undefined

  if (data?.errors) {
    const first = Object.values(data.errors)[0]
    if (first?.length) {
      return first[0]
    }
  }

  return data?.detail || data?.message || data?.title || `请求失败（HTTP ${response?.status ?? 'unknown'}）`
}

/**
 * 素材里的相对路径（例如 /static/classes/ranger.jpg）是「前端」的路径，
 * 管理台自己跑在 5174 上取不到，预览时按前端地址拼一下。
 * 绝对地址（http/https/data）原样返回。
 */
const frontendBase = ((import.meta.env.VITE_FRONTEND_BASE as string | undefined) || 'http://localhost:5173').replace(/\/$/, '')

export function resolveAssetUrl(url?: string | null): string {
  const value = (url ?? '').trim()
  if (!value) {
    return ''
  }

  if (/^(https?:|data:|blob:)/i.test(value)) {
    return value
  }

  return value.startsWith('/') ? `${frontendBase}${value}` : value
}
