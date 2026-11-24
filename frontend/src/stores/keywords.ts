import { reactive } from 'vue'
import { request } from '../api/http'
import type { KeywordEntry } from '../api/types'

/**
 * 关键词词条缓存 + 弹窗栈。
 *
 * - 词条在 App 启动时预加载；之后按 TTL 刷新，打开职业详情等场景会强制刷新，
 *   避免管理台刚加的词条在已经打开过的页面里查不到。
 * - 加载中 / 加载失败时，文本按普通文字渲染（不显示方括号），避免整页看起来坏掉；
 *   只有在词条已加载、且确实没有该词条时，才原样显示 [关键词]。
 * - 点击有词条的关键词会把 key 压进栈，弹窗显示栈顶；「关闭」出栈，「关闭全部」清空。
 */

interface KeywordState {
  ready: boolean
  loading: boolean
  failed: boolean
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
  failed: false,
  map: {},
})

/** 词条缓存有效期：过期后下次使用会重新拉取（不阻塞渲染） */
const TTL_MS = 60_000

/** 失败后多久允许重试 */
const RETRY_MS = 5_000

let pending: Promise<void> | null = null
let lastLoadedAt = 0
let lastAttemptAt = 0
let retryTimer: ReturnType<typeof setTimeout> | null = null

function normalizeKey(key?: string | null): string {
  return (key ?? '').trim().toLowerCase()
}

export function ensureKeywords(force = false): Promise<void> {
  const now = Date.now()

  if (pending) {
    return pending
  }

  if (!force) {
    if (keywordState.ready && now - lastLoadedAt < TTL_MS) {
      return Promise.resolve()
    }

    if (!keywordState.ready && now - lastAttemptAt < RETRY_MS) {
      return Promise.resolve()
    }
  }

  lastAttemptAt = now
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
      keywordState.ready = true
      keywordState.failed = false
      lastLoadedAt = Date.now()
    })
    .catch(() => {
      keywordState.failed = true
      scheduleRetry()
    })
    .finally(() => {
      keywordState.loading = false
      pending = null
    })

  return pending
}

function scheduleRetry(): void {
  if (retryTimer) {
    return
  }

  retryTimer = setTimeout(() => {
    retryTimer = null
    void ensureKeywords(true)
  }, RETRY_MS)
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
