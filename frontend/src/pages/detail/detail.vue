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
        <text class="muted">{{ skill.value }}</text>
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
  </view>

  <view v-else class="page">
    <view class="muted">加载中…</view>
  </view>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { request } from '../../api/http'
import type { CardStatusKey, CharacterDetail } from '../../api/types'
import { formatDateTime } from '../../utils/format'
import AppHeader from '../../components/AppHeader/AppHeader.vue'
import KeywordText from '../../components/KeywordText/KeywordText.vue'

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

/** 「系统 · 职业 · 种族 · 职业 · 年龄」一行，交给关键词组件统一渲染。 */
function metaText(detail: CharacterDetail): string {
  const parts = [detail.systemName, detail.className, detail.raceName, detail.occupation]
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
