import type { Inspiration, SheetAttribute } from '../api/types'

export type CreateStep = 1 | 2 | 3 | 4

export type AttributeMethod = 'roll' | 'standard'

/** 车新卡的本机草稿：车完之前只存在本地，最后一步才提交给服务器。 */
export interface CreateDraft {
  version: 1
  step: CreateStep
  systemCode: string | null
  name: string
  occupation: string
  age: string
  summary: string
  method: AttributeMethod
  attributes: SheetAttribute[]
  inspiration: Inspiration | null
  updatedAt: string
}

const STORAGE_KEY = 'trpg_create_draft'
const DRAFT_VERSION = 1

export function emptyDraft(): CreateDraft {
  return {
    version: DRAFT_VERSION,
    step: 1,
    systemCode: null,
    name: '',
    occupation: '',
    age: '',
    summary: '',
    method: 'roll',
    attributes: [],
    inspiration: null,
    updatedAt: new Date().toISOString(),
  }
}

function asString(value: unknown): string {
  if (typeof value === 'string') {
    return value
  }
  if (typeof value === 'number') {
    return String(value)
  }
  return ''
}

function normalize(raw: unknown): CreateDraft | null {
  if (!raw || typeof raw !== 'object') {
    return null
  }

  const draft = raw as Partial<CreateDraft>
  if (draft.version !== DRAFT_VERSION) {
    return null
  }

  const step = Number(draft.step)
  const attributes = Array.isArray(draft.attributes)
    ? draft.attributes.filter(
        (item): item is SheetAttribute =>
          Boolean(item) && typeof item === 'object' && typeof item.key === 'string',
      )
    : []

  return {
    version: DRAFT_VERSION,
    step: (step >= 1 && step <= 4 ? step : 1) as CreateStep,
    systemCode: typeof draft.systemCode === 'string' && draft.systemCode ? draft.systemCode : null,
    name: asString(draft.name),
    occupation: asString(draft.occupation),
    age: asString(draft.age),
    summary: asString(draft.summary),
    method: draft.method === 'standard' ? 'standard' : 'roll',
    attributes,
    inspiration:
      draft.inspiration && typeof draft.inspiration.title === 'string' ? draft.inspiration : null,
    updatedAt: typeof draft.updatedAt === 'string' ? draft.updatedAt : new Date().toISOString(),
  }
}

export function loadDraft(): CreateDraft | null {
  try {
    const raw = uni.getStorageSync(STORAGE_KEY)
    if (!raw) {
      return null
    }

    return normalize(typeof raw === 'string' ? JSON.parse(raw) : raw)
  } catch {
    return null
  }
}

export function saveDraft(draft: CreateDraft): void {
  try {
    uni.setStorageSync(
      STORAGE_KEY,
      JSON.stringify({
        ...draft,
        updatedAt: new Date().toISOString(),
      }),
    )
  } catch {
    // 本地存储失败不阻断流程
  }
}

export function clearDraft(): void {
  try {
    uni.removeStorageSync(STORAGE_KEY)
  } catch {
    // ignore
  }
}
