import { reactive } from 'vue'
import { request } from '../api/http'
import type { KeywordEntry } from '../api/types'

/**
 * 关键词词条缓存 + 弹窗栈。
 *
 * - 词条在 App 启动时预加载一次；加载失败就整体退化成「原样显示 [关键词]」。
 * - 点击有词条的关键词会压入一个栈项，弹窗按「窗口叠窗口」的方式排队展示：
 *   最新的一层在最前面，后面的层按层数依次往左上退一点，最多同时展示 10 层。
 */

interface KeywordState {
  ready: boolean
  loading: boolean
  map: Record<string, KeywordEntry>
}

export interface KeywordStackItem {
  /** 每次打开的稳定 id，用来给 v-for 当 key，保证层移动时有过渡动画 */
  id: number
  key: string
}

export const keywordState = reactive<KeywordState>({
  ready: false,
  loading: false,
  map: {},
})

let pending: Promise<void> | null = null

function normalizeKey(key?: string | null): string {
  return (key ?? '').trim().toLowerCase()
}

export function ensureKeywords(): Promise<void> {
  if (keywordState.ready) {
    return Promise.resolve()
  }

  if (pending) {
    return pending
  }

  keywordState.loading = true
  pending = request<KeywordEntry[]>({ url: '/api/keywords', auth: false, silent: true })
    .then((list) => {
      const map: Record<string, KeywordEntry> = {}
      for (const item of list ?? []) {
        const key = normalizeKey(item?.key)
        if (key) {
          map[key] = item
        }
      }
      keywordState.map = map
    })
    .catch(() => {
      // 词条加载失败：所有关键词按原样显示，不阻塞页面
    })
    .finally(() => {
      keywordState.ready = true
      keywordState.loading = false
    })

  return pending
}

export function findKeyword(key?: string | null): KeywordEntry | null {
  return keywordState.map[normalizeKey(key)] ?? null
}

/* ---------------- 弹窗栈 ---------------- */

export const keywordStack = reactive<{ items: KeywordStackItem[] }>({ items: [] })

let sequence = 0

export function openKeyword(key: string): void {
  const value = (key ?? '').trim()
  if (!value) {
    return
  }

  sequence += 1
  keywordStack.items.push({ id: sequence, key: value })
}

export function closeKeyword(): void {
  keywordStack.items.pop()
}

export function closeAllKeywords(): void {
  keywordStack.items.splice(0, keywordStack.items.length)
}

export function currentKeywordKey(): string | null {
  const items = keywordStack.items
  return items.length ? items[items.length - 1].key : null
}
