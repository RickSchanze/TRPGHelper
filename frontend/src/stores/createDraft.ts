/*** 车新卡的本机草稿：车完之前只存在本地，最后一步才提交给服务器。 */
export interface ClassSelectionState {
  subclassCode: string | null
  subclassName: string
}

export interface CreateDraft {
  version: 6
  /** 当前步骤序号，从 1 开始；步骤数量由后端的车卡流程配置决定。 */
  step: number
  systemCode: string | null
  classCode: string | null
  className: string
  subclassCode: string | null
  subclassName: string
  /** 每个职业各自保留的选择，来回切职业时不丢 */
  classStates: Record<string, ClassSelectionState>
  updatedAt: string
}

const STORAGE_KEY = 'trpg_create_draft'
const DRAFT_VERSION = 6

export function emptyDraft(): CreateDraft {
  return {
    version: DRAFT_VERSION,
    step: 1,
    systemCode: null,
    classCode: null,
    className: '',
    subclassCode: null,
    subclassName: '',
    classStates: {},
    updatedAt: new Date().toISOString(),
  }
}

function asString(value: unknown): string {
  return typeof value === 'string' ? value : ''
}

function readClassStates(raw: unknown): Record<string, ClassSelectionState> {
  const states: Record<string, ClassSelectionState> = {}
  if (!raw || typeof raw !== 'object') {
    return states
  }

  for (const [code, value] of Object.entries(raw as Record<string, unknown>)) {
    if (!value || typeof value !== 'object') {
      continue
    }

    const item = value as Partial<ClassSelectionState>
    states[code] = {
      subclassCode: typeof item.subclassCode === 'string' && item.subclassCode ? item.subclassCode : null,
      subclassName: asString(item.subclassName),
    }
  }

  return states
}

function normalize(raw: unknown): CreateDraft | null {
  if (!raw || typeof raw !== 'object') {
    return null
  }

  const draft = raw as Partial<CreateDraft>
  const version = Number(draft.version)
  // 5 → 6：只是把当前职业的子职选择补进 classStates
  if (version !== 5 && version !== DRAFT_VERSION) {
    return null
  }

  const step = Number(draft.step)
  const classCode = typeof draft.classCode === 'string' && draft.classCode ? draft.classCode : null
  const classStates = readClassStates(draft.classStates)

  if (classCode && !classStates[classCode] && draft.subclassCode) {
    classStates[classCode] = {
      subclassCode: draft.subclassCode,
      subclassName: asString(draft.subclassName),
    }
  }

  return {
    version: DRAFT_VERSION,
    step: Number.isFinite(step) && step >= 1 ? Math.floor(step) : 1,
    systemCode: typeof draft.systemCode === 'string' && draft.systemCode ? draft.systemCode : null,
    classCode,
    className: asString(draft.className),
    subclassCode: typeof draft.subclassCode === 'string' && draft.subclassCode ? draft.subclassCode : null,
    subclassName: asString(draft.subclassName),
    classStates,
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
