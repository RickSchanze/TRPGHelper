/**
 * 关键词标记：文本里用 [方括号] 包起来的部分是关键词，
 * 前端统一渲染成「斜体 + 粗体」，后续再挂点击/悬浮等交互。
 *
 * 约定：
 * - `[]` 里不能为空，空的方括号按普通文本处理；
 * - 不支持嵌套，`[a[b]` 这种按第一个 `]` 结束；
 * - 未闭合的 `[` 保持原样。
 */

export interface KeywordSegment {
  text: string
  keyword: boolean
}

const KEYWORD_PATTERN = /\[([^\]]+)\]/g

export function splitKeywords(input?: string | null): KeywordSegment[] {
  const text = input ?? ''
  if (!text) {
    return []
  }

  const segments: KeywordSegment[] = []
  let cursor = 0
  let match: RegExpExecArray | null

  KEYWORD_PATTERN.lastIndex = 0
  while ((match = KEYWORD_PATTERN.exec(text)) !== null) {
    if (match.index > cursor) {
      segments.push({ text: text.slice(cursor, match.index), keyword: false })
    }

    segments.push({ text: match[1], keyword: true })
    cursor = match.index + match[0].length
  }

  if (cursor < text.length) {
    segments.push({ text: text.slice(cursor), keyword: false })
  }

  return segments.filter((segment) => segment.text.length > 0)
}

/** 去掉方括号，只留纯文本（导出 / 搜索用）。 */
export function stripKeywords(input?: string | null): string {
  return (input ?? '').replace(KEYWORD_PATTERN, '$1')
}
