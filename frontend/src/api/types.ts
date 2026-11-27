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

export interface SheetProficiencies {
  armor: string[]
  weapons: string[]
  tools: string[]
}

export interface SheetEquipmentItem {
  name: string
  quantity: number
  note?: string
}

export interface SheetCombat {
  hitPoints: number
  armorClass: number
  initiative: number
  speed: number
  proficiencyBonus: number
  passivePerception: number
  savingThrows: Record<string, number>
}

export interface SheetPersonality {
  trait?: string
  ideal?: string
  bond?: string
  flaw?: string
}

export interface CharacterSheet {
  attributes: SheetAttribute[]
  skills: SheetSkill[]
  items: string[]
  background?: string
  notes?: string
  backgroundName?: string
  backgroundFeatureName?: string
  backgroundFeatureText?: string
  gender?: string
  height?: string
  weight?: string
  appearance?: string
  languages: string[]
  proficiencies: SheetProficiencies
  equipment: SheetEquipmentItem[]
  combat: SheetCombat
  personality: SheetPersonality
  featureChoices: Record<string, string>
}

export interface CharacterSummary {
  id: string
  name: string
  occupation?: string
  className?: string
  subclassName?: string
  raceName?: string
  subraceName?: string
  /** 阵营，例如「守序善良」 */
  alignmentName?: string
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
  /** 职业的豁免熟练，例如「力量,体质」 */
  savingThrows?: string
  /** 1 级即可施法时的施法属性，例如「魅力」；空表示 1 级还不施法 */
  spellcastingAbility?: string
  features: string[]
  /** 管理台配置的职业配图，可能为空（为空时用主色块） */
  imageUrl?: string
  /** 管理台配置的主色，例如 #3f5a3a */
  accent?: string
  /** 从职业技能列表里选几项 */
  skillChoiceCount: number
  /** 可选的职业技能名单 */
  skillOptions: string[]
  armorProficiencies: string[]
  weaponProficiencies: string[]
}

/** 18 项技能之一。 */
export interface SkillOption {
  id: string
  name: string
  ability: string
  description: string
}

export interface LanguageOption {
  id: string
  name: string
  kind: string
  description: string
}

export interface BackgroundDetail {
  id: string
  name: string
  description: string
  skills: string[]
  tools: string
  languageCount: number
  equipment: string
  featureName: string
  featureText: string
}

/** 1 级职业特性选项。 */
export interface FeatureOption {
  classId: string
  optionType: string
  name: string
  description: string
}

export interface ArmorOption {
  id: string
  name: string
  type: string
  baseAc: number
  dexCap?: number
  strengthReq?: number
  stealthDisadvantage: boolean
  description: string
}

export interface WeaponOption {
  id: string
  name: string
  damage: string
  damageType: string
  ability: string
  ranged: boolean
  properties: string
}

export interface EquipmentKit {
  classId: string
  armorId: string
  shield: boolean
  weaponIds: string[]
  defaultWeaponId: string
  pack: string
}

export interface EquipmentResponse {
  armor: ArmorOption[]
  weapons: WeaponOption[]
  kit: EquipmentKit | null
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
  traits: string[]
  /** 结构化加值（「敏捷 +2」），决定属性时用来算来源 */
  abilityBonuses: AbilityBonus[]
  /** 需要玩家自己选的部分（半精灵「任选两项 +1」） */
  abilityChoices: AbilityChoice[]
  /** 种族直接给的技能熟练 */
  skills: string[]
  /** 种族允许自选的技能数量（半精灵 2） */
  skillChoiceCount: number
  /** 种族母语 */
  languages: string[]
  /** 额外语言数量 */
  languageChoiceCount: number
}

export interface SubraceOption {
  code: string
  name: string
  raceId: string
  description: string
  traits: string[]
  abilityBonuses: AbilityBonus[]
  abilityChoices: AbilityChoice[]
}

/** 种族 / 亚种给的固定属性加值 */
export interface AbilityBonus {
  key: string
  label: string
  value: number
}

/** 需要玩家自己选的属性加值（例如半精灵「任选两项 +1」） */
export interface AbilityChoice {
  id: string
  label: string
  count: number
  value: number
}

/** 「决定属性」一次生成：基础值 + 掷骰明细 */
export interface AbilityScoreRoll {
  key: string
  label: string
  base: number
  formula: string
  dice: number[]
  kept: number[]
}

export interface AbilityScoresResponse {
  systemCode: string
  method: string
  attributes: AbilityScoreRoll[]
}

/** 阵营（九宫格）：解释文字在关键词里，这里只有渲染图标 + 名字要用的字段。 */
export interface AlignmentOption {
  code: string
  name: string
  iconUrl?: string
  accent?: string
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
