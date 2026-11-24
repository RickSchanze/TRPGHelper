export type CardStatusKey = 'draft' | 'ready' | 'inUse' | 'retained' | 'archived'

export interface UserDto {
  id: string
  nickName: string
  avatarUrl?: string
  role: string
  createdAt: string
  lastLoginAt: string
  totalCards: number
  retainedCards: number
}

export interface LoginResponse {
  token: string
  expiresAt: string
  user: UserDto
}

export interface AttributeTemplate {
  key: string
  label: string
  formula: string
}

export interface GameSystem {
  id: string
  code: string
  name: string
  description?: string
  iconUrl?: string
  accent?: string
  available: boolean
  attributes: AttributeTemplate[]
}

export interface SheetAttribute {
  key: string
  label: string
  value: number
  formula?: string
}

export interface SheetSkill {
  name: string
  value: number
  note?: string
}

export interface CharacterSheet {
  attributes: SheetAttribute[]
  skills: SheetSkill[]
  items: string[]
  background?: string
  notes?: string
}

export interface CharacterSummary {
  id: string
  name: string
  occupation?: string
  className?: string
  subclassName?: string
  raceName?: string
  age?: number
  summary?: string
  portraitUrl?: string
  systemId: string
  systemCode: string
  systemName: string
  status: CardStatusKey
  statusLabel: string
  isFavorite: boolean
  createdAt: string
  updatedAt: string
  retainedAt?: string
}

export interface CardLog {
  id: string
  action: string
  actionLabel: string
  note?: string
  createdAt: string
}

export interface CharacterDetail extends CharacterSummary {
  sheet: CharacterSheet
  logs: CardLog[]
  nextStatuses: CardStatusKey[]
  nextStatusLabels: string[]
}

export interface CharacterStats {
  total: number
  draft: number
  ready: number
  inUse: number
  retained: number
  archived: number
  createdLast30Days: number
}

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface GeneratedAttributes {
  systemId: string
  systemCode: string
  systemName: string
  method: string
  attributes: SheetAttribute[]
  total: number
}

export interface Inspiration {
  title: string
  text: string
}

export interface ClassOption {
  code: string
  name: string
  description: string
  hitDie: string
  primaryAbility: string
  features: string[]
  /** 管理台配置的职业配图，可能为空（为空时用主色块） */
  imageUrl?: string
  /** 管理台配置的主色，例如 #3f5a3a */
  accent?: string
}

/** 关键词词条：点击 [关键词] 时弹出的解释。 */
export interface KeywordEntry {
  key: string
  title?: string
  summary: string
  content: string
  imageUrl?: string
}

/** 职业等级成长：某一级获得的一条特性。 */
export interface ClassLevelFeature {
  level: number
  title: string
  description: string
}

export interface ClassLevels {
  systemCode: string
  classCode: string
  className: string
  features: ClassLevelFeature[]
}

/** 车卡流程的一步，由管理台配置；规则书选择不在其中。 */
export interface CreationStep {
  id: string
  kind: string
  title: string
  description?: string
}

export interface CreationFlow {
  systemCode?: string
  systemName?: string
  steps: CreationStep[]
}

export interface RaceOption {
  code: string
  name: string
  description: string
  abilityBonus: string
  speed: number
}

/** 子职选项（圣武士的三个神圣誓约等）。 */
export interface SubclassOption {
  code: string
  name: string
  classId: string
  description: string
  features: string[]
  imageUrl?: string
  accent?: string
}
