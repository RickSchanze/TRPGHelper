import { reactive } from 'vue'
import { clearToken, getToken, request, setToken } from '../api/http'
import type { LoginResponse, UserDto } from '../api/types'

export const session = reactive<{ user: UserDto | null }>({ user: null })

export function isLoggedIn(): boolean {
  return Boolean(getToken())
}

export function applyLogin(result: LoginResponse): void {
  setToken(result.token)
  session.user = result.user
}

export async function refreshMe(): Promise<UserDto | null> {
  if (!getToken()) {
    session.user = null
    return null
  }

  try {
    session.user = await request<UserDto>({ url: '/api/auth/me', silent: true })
    return session.user
  } catch {
    session.user = null
    return null
  }
}

export function logout(): void {
  clearToken()
  session.user = null
}

export function goLogin(): void {
  uni.reLaunch({ url: '/pages/login/login' })
}
