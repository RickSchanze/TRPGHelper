import { API_BASE_URL } from '../api/http'

/**
 * 把素材里的图片地址解析成能直接给 <image src> 用的地址：
 * - http(s) / data / blob 原样返回；
 * - /uploads 开头的是后端上传目录，补上 API 地址；
 * - /static 开头的是前端打包进包里的资源，保持相对路径。
 */
export function resolveMediaUrl(url?: string | null): string {
  const value = (url ?? '').trim()
  if (!value) {
    return ''
  }

  if (/^(https?:|data:|blob:)/i.test(value)) {
    return value
  }

  if (value.startsWith('/uploads')) {
    return `${API_BASE_URL.replace(/\/$/, '')}${value}`
  }

  return value
}
