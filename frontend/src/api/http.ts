const BASE_URL: string = import.meta.env.VITE_API_BASE || 'http://localhost:5080'
const TOKEN_KEY = 'trpg_token'

export interface RequestOptions {
  url: string
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE'
  data?: unknown
  auth?: boolean
  /** 失败时不弹 toast（用于后台刷新） */
  silent?: boolean
}

export function getToken(): string {
  return (uni.getStorageSync(TOKEN_KEY) as string) || ''
}

export function setToken(token: string): void {
  uni.setStorageSync(TOKEN_KEY, token)
}

export function clearToken(): void {
  uni.removeStorageSync(TOKEN_KEY)
}

function extractMessage(data: unknown): string {
  if (!data || typeof data !== 'object') {
    return ''
  }

  const body = data as { detail?: string; title?: string; errors?: Record<string, string[]> }
  if (body.errors) {
    const first = Object.values(body.errors)[0]
    if (first && first.length > 0) {
      return first[0]
    }
  }

  return body.detail || body.title || ''
}

export function request<T>(options: RequestOptions): Promise<T> {
  const useAuth = options.auth !== false

  return new Promise<T>((resolve, reject) => {
    uni.request({
      url: BASE_URL + options.url,
      method: options.method || 'GET',
      data: options.data as Record<string, unknown> | undefined,
      header: {
        'Content-Type': 'application/json',
        ...(useAuth && getToken() ? { Authorization: `Bearer ${getToken()}` } : {}),
      },
      success: (response) => {
        const status = response.statusCode ?? 0

        if (status >= 200 && status < 300) {
          resolve(response.data as T)
          return
        }

        if (status === 401) {
          clearToken()
          uni.reLaunch({ url: '/pages/login/login' })
        }

        const message = extractMessage(response.data) || `请求失败（${status}）`
        if (!options.silent) {
          uni.showToast({ title: message, icon: 'none' })
        }
        reject({ status, message })
      },
      fail: (error) => {
        if (!options.silent) {
          uni.showToast({ title: '连不上后端，请确认 API 已启动', icon: 'none' })
        }
        reject({ status: 0, message: error.errMsg })
      },
    })
  })
}
