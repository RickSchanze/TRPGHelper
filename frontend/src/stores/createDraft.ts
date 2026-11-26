/*** 车新卡的本机草稿：车完之前只存在本地，最后一步才提交给服务器。 */
export interface ClassSelectionState {
  subclassCode: string | null
  subclassName: string
}

export interface CreateDraft {
  version: 13
  /** 当前步骤序号，从 1 开始；步骤数量由后端的车卡流程配置决定。 */
  step: number
  systemCode: string | null
  classCode: string | null
  className: string
  subclassCode: string | null
  subclassName: string
  /** 种族不跟随职业，单独存一份 */
  raceCode: string | null
  raceName: string
  /** 亚种跟随种族，切换种族时清空 */
  subraceCode: string | null
  subraceName: string
  /** 阵营在「决定背景」这一步里选，和职业 / 种族无关 */
  alignmentCode: string | null
  alignmentName: string
  /** 决定属性：开局是空的，六项都要自己填；「投掷」只是给一组参考数值 */
  attributeValues: Record<string, number>
  /** 自选加值：race = 半精灵那类「任选两项 +1」；housePlus2 / housePlus1 = 任意两项 +2 / +1 */
  attributeChoices: Record<string, string[]>
  /** 可选规则：种族属性加值改为任意两项 +2 / +1 */
  houseRuleAbilityScores: boolean
  /** 每个职业各自保留的子职选择，来回切职业时不丢 */
  classStates: Record<string, ClassSelectionState>
  updatedAt: string
}

const STORAGE_KEY = 'trpg_create_draft'
const DRAFT_VERSION = 13

export function emptyDraft(): CreateDraft {
  return {
    version: DRAFT_VERSION,
    step: 1,
    systemCode: null,
    classCode: null,
    className: '',
    subclassCode: null,
    subclassName: '',
    raceCode: null,
    raceName: '',
    subraceCode: null,
    subraceName: '',
    alignmentCode: null,
    alignmentName: '',
    attributeValues: {},
    attributeChoices: {},
    houseRuleAbilityScores: false,
    classStates: {},
    updatedAt: new Date().toISOString(),
  }
}

function asString(value: unknown): string {
  return typeof value === 'string' ? value : ''
}

function asCode(value: unknown): string | null {
  return typeof value === 'string' && value ? value : null
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
      subclassCode: asCode(item.subclassCode),
      subclassName: asString(item.subclassName),
    }
  }

  return states
}

function readNumberMap(raw: unknown): Record<string, number> {
  const map: Record<string, number> = {}
  if (!raw || typeof raw !== 'object') {
    return map
  }

  for (const [key, value] of Object.entries(raw as Record<string, unknown>)) {
    const number = Number(value)
    if (Number.isFinite(number)) {
      map[key] = Math.floor(number)
    }
  }

  return map
}

function readStringListMap(raw: unknown): Record<string, string[]> {
  const map: Record<string, string[]> = {}
  if (!raw || typeof raw !== 'object') {
    return map
  }

  for (const [key, value] of Object.entries(raw as Record<string, unknown>)) {
    if (!Array.isArray(value)) {
      continue
    }

    map[key] = value.filter((item): item is string => typeof item === 'string' && item.length > 0)
  }

  return map
}

function normalize(raw: unknown): CreateDraft | null {
  if (!raw || typeof raw !== 'object') {
    return null
  }

  const draft = raw as Partial<CreateDraft> & { version?: number; attributeBase?: unknown }
  const version = Number(draft.version)
  // 5 - 13：旧草稿都能升上来，缺的字段补默认值；v13 起属性只存「初始数值」，v12 的 attributeBase 直接搬过来
  if (![5, 6, 7, 8, 9, 10, 11, 12, DRAFT_VERSION].includes(version)) {
    return null
  }

  const step = Number(draft.step)
  const classCode = asCode(draft.classCode)
  const classStates = readClassStates(draft.classStates)

  if (classCode && !classStates[classCode]) {
    classStates[classCode] = {
      subclassCode: asCode(draft.subclassCode),
      subclassName: asString(draft.subclassName),
    }
  }

  return {
    version: DRAFT_VERSION,
    step: Number.isFinite(step) && step >= 1 ? Math.floor(step) : 1,
    systemCode: asCode(draft.systemCode),
    classCode,
    className: asString(draft.className),
    subclassCode: asCode(draft.subclassCode),
    subclassName: asString(draft.subclassName),
    raceCode: asCode(draft.raceCode),
    raceName: asString(draft.raceName),
    subraceCode: asCode(draft.subraceCode),
    subraceName: asString(draft.subraceName),
    alignmentCode: asCode(draft.alignmentCode),
    alignmentName: asString(draft.alignmentName),
    attributeValues: readNumberMap(draft.attributeValues ?? draft.attributeBase),
    attributeChoices: readStringListMap(draft.attributeChoices),
    houseRuleAbilityScores: draft.houseRuleAbilityScores === true,
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
