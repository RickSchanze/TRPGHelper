<template>
  <view v-if="card" class="page">
    <AppHeader current="detail" />

    <view class="head">
      <view class="head-main">
        <view class="name">
          <KeywordText :text="card.name" />
        </view>
        <view class="muted meta">
          <KeywordText :text="metaText(card)" />
        </view>
      </view>
      <text class="badge" :class="'badge-' + card.status">{{ card.statusLabel }}</text>
    </view>

    <view v-if="card.summary" class="summary">
      <KeywordText :text="card.summary" />
    </view>

    <view class="actions">
      <view v-for="status in card.nextStatuses" :key="status" class="action" @tap="changeStatus(status)">
        {{ actionLabel(status) }}
      </view>
      <view class="action action-danger" @tap="remove">删除</view>
    </view>

    <view class="section">
      <view class="section-title">属性</view>
      <view v-if="card.sheet.attributes.length === 0" class="muted">还没有属性。</view>
      <view class="attr-grid">
        <view v-for="attribute in card.sheet.attributes" :key="attribute.key" class="attr">
          <view class="attr-value">{{ attribute.value }}</view>
          <view class="attr-label muted">
            <KeywordText :text="attribute.label" />
          </view>
          <view v-if="attribute.formula" class="attr-formula muted">
            <KeywordText :text="attribute.formula" />
          </view>
        </view>
      </view>
    </view>

    <view v-if="card.sheet.skills.length > 0" class="section">
      <view class="section-title">技能</view>
      <view v-for="skill in card.sheet.skills" :key="skill.name" class="skill">
        <KeywordText :text="skill.name" />
        <text class="muted">{{ skill.value }}{{ skill.note ? ` · ${skill.note}` : '' }}</text>
      </view>
    </view>

    <view v-if="combat" class="section">
      <view class="section-title">战斗</view>
      <view class="stat-row">
        <view class="stat"><view class="stat-value">{{ combat.hitPoints }}</view><view class="muted stat-label">生命上限</view></view>
        <view class="stat"><view class="stat-value">{{ combat.armorClass }}</view><view class="muted stat-label">AC</view></view>
        <view class="stat"><view class="stat-value">{{ signed(combat.initiative) }}</view><view class="muted stat-label">先攻</view></view>
        <view class="stat"><view class="stat-value">{{ combat.speed }}</view><view class="muted stat-label">速度</view></view>
        <view class="stat"><view class="stat-value">{{ signed(combat.proficiencyBonus) }}</view><view class="muted stat-label">熟练</view></view>
        <view class="stat"><view class="stat-value">{{ combat.passivePerception }}</view><view class="muted stat-label">被动察觉</view></view>
      </view>
      <view v-if="savingThrowEntries.length" class="detail-row">
        <text class="muted">豁免</text>
        <text>{{ savingThrowEntries.map(([name, value]) => `${name} ${signed(value)}`).join(' · ') }}</text>
      </view>
    </view>

    <view v-if="hasDescription" class="section">
      <view class="section-title">背景与描述</view>
      <view v-if="card.sheet.backgroundName" class="detail-row">
        <text class="muted">背景</text><text>{{ card.sheet.backgroundName }}</text>
      </view>
      <view v-if="card.sheet.backgroundFeatureName" class="detail-row">
        <text class="muted">背景特性</text>
        <text>{{ card.sheet.backgroundFeatureName }}{{ card.sheet.backgroundFeatureText ? `：${card.sheet.backgroundFeatureText}` : '' }}</text>
      </view>
      <view v-if="card.sheet.gender" class="detail-row">
        <text class="muted">性别</text><text>{{ card.sheet.gender }}</text>
      </view>
      <view v-if="card.age" class="detail-row">
        <text class="muted">年龄</text><text>{{ card.age }}</text>
      </view>
      <view v-if="card.sheet.height" class="detail-row">
        <text class="muted">身高</text><text>{{ card.sheet.height }}</text>
      </view>
      <view v-if="card.sheet.weight" class="detail-row">
        <text class="muted">体重</text><text>{{ card.sheet.weight }}</text>
      </view>
      <view v-if="card.sheet.appearance" class="detail-row">
        <text class="muted">外貌</text><text>{{ card.sheet.appearance }}</text>
      </view>
      <view v-if="card.sheet.personality.trait" class="detail-row">
        <text class="muted">性格</text><text>{{ card.sheet.personality.trait }}</text>
      </view>
      <view v-if="card.sheet.personality.ideal" class="detail-row">
        <text class="muted">理想</text><text>{{ card.sheet.personality.ideal }}</text>
      </view>
      <view v-if="card.sheet.personality.bond" class="detail-row">
        <text class="muted">牵绊</text><text>{{ card.sheet.personality.bond }}</text>
      </view>
      <view v-if="card.sheet.personality.flaw" class="detail-row">
        <text class="muted">缺点</text><text>{{ card.sheet.personality.flaw }}</text>
      </view>
    </view>

    <view v-if="card.sheet.languages.length" class="section">
      <view class="section-title">语言</view>
      <view class="detail-row"><text>{{ card.sheet.languages.join('、') }}</text></view>
    </view>

    <view v-if="proficiencyEntries.length" class="section">
      <view class="section-title">熟练项</view>
      <view v-for="entry in proficiencyEntries" :key="entry.label" class="detail-row">
        <text class="muted">{{ entry.label }}</text><text>{{ entry.value.join('、') }}</text>
      </view>
    </view>

    <view v-if="card.sheet.equipment.length" class="section">
      <view class="section-title">装备</view>
      <view v-for="item in card.sheet.equipment" :key="item.name" class="detail-row">
        <text>{{ item.name }} ×{{ item.quantity }}</text><text class="muted">{{ item.note }}</text>
      </view>
    </view>

    <view v-if="featureEntries.length" class="section">
      <view class="section-title">特性选择</view>
      <view v-for="entry in featureEntries" :key="entry.label" class="detail-row">
        <text class="muted">{{ entry.label }}</text><text>{{ entry.value }}</text>
      </view>
    </view>

    <view class="section">
      <view class="section-title">记录</view>
      <view v-if="card.logs.length === 0" class="muted">还没有记录。</view>
      <view v-for="log in card.logs" :key="log.id" class="log">
        <view class="log-head">
          <text class="log-action">{{ log.actionLabel }}</text>
          <text class="muted log-time">{{ formatDateTime(log.createdAt) }}</text>
        </view>
        <view v-if="log.note" class="log-note">
          <KeywordText :text="log.note" />
        </view>
      </view>
    </view>

    <KeywordModal />
  </view>

  <view v-else class="page">
    <view class="muted">加载中…</view>
  </view>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { request } from '../../api/http'
import type { CardStatusKey, CharacterDetail } from '../../api/types'
import { formatDateTime } from '../../utils/format'
import AppHeader from '../../components/AppHeader/AppHeader.vue'
import KeywordText from '../../components/KeywordText/KeywordText.vue'
import KeywordModal from '../../components/KeywordModal/KeywordModal.vue'

const card = ref<CharacterDetail | null>(null)
const cardId = ref('')

const labels: Record<CardStatusKey, string> = {
  draft: '退回草稿',
  ready: '转为待用',
  inUse: '开始使用',
  retained: '留卡',
  archived: '归档',
}

function actionLabel(status: CardStatusKey): string {
  return labels[status] ?? status
}

function signed(value: number): string {
  return value >= 0 ? `+${value}` : String(value)
}

const combat = computed(() => {
  const value = card.value?.sheet.combat
  return value && value.hitPoints > 0 ? value : null
})

const savingThrowEntries = computed(() => Object.entries(combat.value?.savingThrows ?? {}))

const FEATURE_LABELS: Record<string, string> = {
  fightingStyle: '战斗风格',
  favoredEnemy: '宿敌',
  naturalExplorer: '自然探索者',
}

const featureEntries = computed(() =>
  Object.entries(card.value?.sheet.featureChoices ?? {}).map(([key, value]) => ({
    label: FEATURE_LABELS[key] ?? key,
    value,
  })),
)

const proficiencyEntries = computed(() => {
  const proficiencies = card.value?.sheet.proficiencies
  if (!proficiencies) {
    return []
  }

  return [
    { label: '护甲', value: proficiencies.armor },
    { label: '武器', value: proficiencies.weapons },
    { label: '工具', value: proficiencies.tools },
  ].filter((entry) => entry.value.length > 0)
})

const hasDescription = computed(() => {
  const sheet = card.value?.sheet
  if (!sheet) {
    return false
  }

  return Boolean(
    sheet.backgroundName ||
      sheet.gender ||
      sheet.height ||
      sheet.weight ||
      sheet.appearance ||
      sheet.personality?.trait ||
      sheet.personality?.ideal ||
      sheet.personality?.bond ||
      sheet.personality?.flaw,
  )
})

/** 「系统 · 职业 · 子职 · 种族 · 亚种 · 阵营 · 职业 · 年龄」一行，交给关键词组件统一渲染。 */
function metaText(detail: CharacterDetail): string {
  const parts = [detail.systemName, detail.className, detail.subclassName, detail.raceName, detail.subraceName, detail.alignmentName, detail.occupation]
    .filter((part): part is string => Boolean(part && part.trim()))
  const text = parts.join(' · ')
  return detail.age ? `${text} · ${detail.age} 岁` : text
}

async function load(): Promise<void> {
  if (!cardId.value) {
    return
  }

  try {
    card.value = await request<CharacterDetail>({ url: `/api/characters/${cardId.value}` })
  } catch {
    // ignore
  }
}

function changeStatus(status: CardStatusKey): void {
  if (!card.value) {
    return
  }

  uni.showModal({
    title: '改变状态',
    content: `确定把「${card.value.name}」改为「${actionLabel(status)}」吗？`,
    success: async (result) => {
      if (!result.confirm || !card.value) {
        return
      }

      try {
        card.value = await request<CharacterDetail>({
          url: `/api/characters/${card.value.id}/status`,
          method: 'POST',
          data: { status },
        })
        uni.showToast({ title: '已更新', icon: 'none' })
      } catch {
        // ignore
      }
    },
  })
}

function remove(): void {
  if (!card.value) {
    return
  }

  const target = card.value.id
  uni.showModal({
    title: '删除角色卡',
    content: '删除后不可恢复，确定吗？',
    confirmColor: '#b4473c',
    success: async (result) => {
      if (!result.confirm) {
        return
      }

      try {
        await request<void>({ url: `/api/characters/${target}`, method: 'DELETE' })
        uni.navigateBack()
      } catch {
        // ignore
      }
    },
  })
}

onLoad((options) => {
  cardId.value = options?.id ?? ''
  void load()
})
</script>

<style lang="scss" scoped>
.head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  margin-bottom: 16rpx;
}

.name {
  font-size: 48rpx;
  font-weight: 600;
}

.meta {
  margin-top: 8rpx;
  font-size: 24rpx;
}

.summary {
  margin: 12rpx 0 24rpx;
  font-size: 26rpx;
  color: #4a4238;
}

.actions {
  display: flex;
  flex-wrap: wrap;
  gap: 16rpx;
  margin-bottom: 32rpx;
}

.action {
  padding: 14rpx 28rpx;
  border-radius: 999rpx;
  border: 2rpx solid $trpg-accent;
  color: $trpg-accent;
  font-size: 24rpx;
  background: #fffdf8;
}

.action:active {
  background: $trpg-accent;
  color: #fffaf0;
}

.action-danger {
  border-color: #c9a49d;
  color: #b4473c;
}

.section {
  background: #fffdf8;
  border: 2rpx solid $trpg-line;
  border-radius: 20rpx;
  padding: 28rpx;
  margin-bottom: 24rpx;
}

.section-title {
  font-size: 30rpx;
  font-weight: 600;
  margin-bottom: 16rpx;
}

.attr-grid {
  display: flex;
  flex-wrap: wrap;
}

.attr {
  width: 33.33%;
  text-align: center;
  padding: 16rpx 0;
}

.attr-value {
  font-size: 44rpx;
  font-weight: 600;
  color: $trpg-accent-deep;
}

.attr-label {
  font-size: 22rpx;
}

.attr-formula {
  font-size: 18rpx;
}

.stat-row {
  display: flex;
  flex-wrap: wrap;
  margin-top: 4rpx;
}

.stat {
  width: 33.33%;
  padding: 10rpx 0;
  text-align: center;
}

.stat-value {
  font-size: 40rpx;
  font-weight: 600;
  color: $trpg-accent-deep;
}

.stat-label {
  font-size: 20rpx;
}

.detail-row {
  display: flex;
  justify-content: space-between;
  gap: 16rpx;
  padding: 12rpx 0;
  border-bottom: 2rpx solid $trpg-line;
  font-size: 24rpx;
}

.detail-row:last-child {
  border-bottom: none;
}

.skill,
.log {
  padding: 16rpx 0;
  border-bottom: 2rpx solid $trpg-line;
}

.skill {
  display: flex;
  justify-content: space-between;
}

.skill:last-child,
.log:last-child {
  border-bottom: none;
}

.log-head {
  display: flex;
  justify-content: space-between;
  font-size: 24rpx;
}

.log-action {
  color: $trpg-accent-deep;
}

.log-time {
  font-size: 22rpx;
}

.log-note {
  margin-top: 6rpx;
  font-size: 24rpx;
  color: #4a4238;
}

@media (min-width: 900px) {
  .page {
    max-width: 900px;
  }

  .head {
    margin-bottom: 12px;
  }

  .name {
    font-size: 34px;
  }

  .meta {
    font-size: 13px;
    margin-top: 6px;
  }

  .summary {
    font-size: 14px;
    margin: 10px 0 20px;
  }

  .actions {
    gap: 12px;
    margin-bottom: 28px;
  }

  .action {
    font-size: 13px;
    padding: 8px 18px;
    border-width: 1px;
    cursor: pointer;
  }

  .section {
    border-width: 1px;
    border-radius: 14px;
    padding: 24px;
    margin-bottom: 20px;
  }

  .section-title {
    font-size: 17px;
    margin-bottom: 14px;
  }

  .attr {
    width: 20%;
    padding: 14px 0;
  }

  .attr-value {
    font-size: 30px;
  }

  .attr-label {
    font-size: 12px;
  }

  .attr-formula {
    font-size: 11px;
  }

  .stat {
    width: 16.66%;
    padding: 8px 0;
  }

  .stat-value {
    font-size: 24px;
  }

  .stat-label {
    font-size: 12px;
  }

  .detail-row {
    padding: 8px 0;
    border-bottom-width: 1px;
    font-size: 13px;
  }

  .skill,
  .log {
    padding: 12px 0;
    border-bottom-width: 1px;
  }

  .log-head {
    font-size: 13px;
  }

  .log-time {
    font-size: 12px;
  }

  .log-note {
    font-size: 13px;
  }
}
</style>
