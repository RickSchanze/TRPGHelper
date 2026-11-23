export type CardStatusKey = 'draft' | 'ready' | 'inUse' | 'retained' | 'archived'

export interface UserDto {
  id: string
  nickName: string
  avatarUrl?: string
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
}

export interface RaceOption {
  code: string
  name: string
  description: string
  abilityBonus: string
  speed: number
}
