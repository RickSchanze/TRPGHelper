/*** 车新卡的本机草稿：车完之前只存在本地，最后一步才提交给服务器。 */
export interface CreateDraft {
  version: 5
  /** 当前步骤序号，从 1 开始；步骤数量由后端的车卡流程配置决定。 */
  step: number
  systemCode: string | null
  classCode: string | null
  className: string
  subclassCode: string | null
  subclassName: string
  updatedAt: string
}

const STORAGE_KEY = 'trpg_create_draft'
const DRAFT_VERSION = 5

export function emptyDraft(): CreateDraft {
  return {
    version: DRAFT_VERSION,
    step: 1,
    systemCode: null,
    classCode: null,
    className: '',
    subclassCode: null,
    subclassName: '',
    updatedAt: new Date().toISOString(),
  }
}

function asString(value: unknown): string {
  return typeof value === 'string' ? value : ''
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

  return {
    version: DRAFT_VERSION,
    step: Number.isFinite(step) && step >= 1 ? Math.floor(step) : 1,
    systemCode: typeof draft.systemCode === 'string' && draft.systemCode ? draft.systemCode : null,
    classCode: typeof draft.classCode === 'string' && draft.classCode ? draft.classCode : null,
    className: asString(draft.className),
    subclassCode: typeof draft.subclassCode === 'string' && draft.subclassCode ? draft.subclassCode : null,
    subclassName: asString(draft.subclassName),
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
