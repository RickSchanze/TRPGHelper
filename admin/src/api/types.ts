export interface AdminUser {
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
  user: AdminUser
}

export interface AttributeTemplate {
  key: string
  label: string
  formula: string
}

export interface AdminSystem {
  id: string
  code: string
  name: string
  description?: string
  nameTemplate: string
  isBuiltin: boolean
  sortOrder: number
  attributes: AttributeTemplate[]
}

export interface SaveSystemPayload {
  code: string
  name: string
  description?: string
  nameTemplate: string
  sortOrder: number
  attributes: AttributeTemplate[]
}

export type GeneratorKindKey =
  | 'namePart'
  | 'occupation'
  | 'background'
  | 'class'
  | 'subclass'
  | 'race'
  | 'inspiration'

export interface AdminGeneratorEntry {
  id: string
  systemCode?: string | null
  kind: GeneratorKindKey
  category: string
  value: string
  extra?: Record<string, unknown> | null
  sortOrder: number
  isEnabled: boolean
  createdAt: string
  updatedAt: string
}

export interface SaveGeneratorEntryPayload {
  systemCode?: string | null
  kind: GeneratorKindKey
  category: string
  value: string
  extra?: Record<string, unknown> | null
  sortOrder: number
  isEnabled: boolean
}

export interface PagedResult<T> {
  items: T[]
  total: number
  page: number
  pageSize: number
}

export interface AdminKindCount {
  kind: GeneratorKindKey
  count: number
}

export type CreationStepKindKey = 'class' | 'subclass' | 'race' | 'subrace' | 'attributes' | 'name' | 'summary'

export interface AdminCreationStep {
  id: string
  systemCode?: string | null
  kind: CreationStepKindKey
  title: string
  description?: string | null
  sortOrder: number
  isEnabled: boolean
  createdAt: string
  updatedAt: string
}

export interface SaveCreationStepPayload {
  systemCode?: string | null
  kind: CreationStepKindKey
  title: string
  description?: string | null
  sortOrder: number
  isEnabled: boolean
}

export interface AdminKeyword {
  id: string
  code: string
  key: string
  category: string
  title?: string | null
  summary: string
  content: string
  imageUrl?: string | null
  sortOrder: number
  isEnabled: boolean
  createdAt: string
  updatedAt: string
}

export interface SaveKeywordPayload {
  key: string
  category: string
  title?: string | null
  summary: string
  content: string
  imageUrl?: string | null
  sortOrder: number
  isEnabled: boolean
}

export interface AdminClassLevelFeature {
  id: string
  systemCode: string
  classCode: string
  level: number
  title: string
  description: string
  sortOrder: number
  isEnabled: boolean
  updatedAt: string
}

export interface SaveClassLevelItem {
  level: number
  title: string
  description: string
  sortOrder: number
  isEnabled: boolean
}

export interface CreationStepPreview {
  id: string
  kind: CreationStepKindKey
  title: string
  description?: string | null
}

export interface CreationFlowPreview {
  systemCode?: string | null
  systemName?: string | null
  steps: CreationStepPreview[]
}

export interface AdminOverview {
  systems: number
  generatorEntries: number
  enabledGeneratorEntries: number
  users: number
  admins: number
  cards: number
  entriesByKind: AdminKindCount[]
  generatedAt: string
}
