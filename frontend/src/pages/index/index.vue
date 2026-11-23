<template>
  <view class="page">
    <view class="hero">
      <view class="hero-title">卡库</view>
      <view class="hero-sub">已车 {{ stats?.total ?? 0 }} 张 · 留存 {{ stats?.retained ?? 0 }} 张</view>
    </view>

    <view class="search">
      <input
        v-model="keyword"
        class="search-input"
        placeholder="搜角色名 / 职业 / 简介"
        confirm-type="search"
        @confirm="load"
      />
    </view>

    <scroll-view scroll-x class="filters" :show-scrollbar="false">
      <view
        v-for="item in filters"
        :key="item.key"
        class="chip"
        :class="{ 'chip-active': active === item.key }"
        @tap="setFilter(item.key)"
      >
        {{ item.label }}
      </view>
    </scroll-view>

    <view v-if="!loading && items.length === 0" class="empty">
      <view class="empty-title">这里还空着</view>
      <view class="empty-text">点右下角「车新卡」，先掷一组属性。</view>
    </view>

    <view v-for="card in items" :key="card.id" class="card" @tap="openDetail(card.id)">
      <view class="card-head">
        <text class="card-name">{{ card.name }}</text>
        <text class="badge" :class="'badge-' + card.status">{{ card.statusLabel }}</text>
      </view>
      <view class="card-meta muted">
        <text>{{ card.systemName }}</text>
        <text v-if="card.occupation"> · {{ card.occupation }}</text>
        <text v-if="card.age"> · {{ card.age }} 岁</text>
      </view>
      <view v-if="card.summary" class="card-summary">{{ card.summary }}</view>
      <view v-if="card.retainedAt" class="card-foot muted">留存于 {{ formatDate(card.retainedAt) }}</view>
    </view>

    <view class="fab" @tap="goCreate">＋ 车新卡</view>
  </view>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { getToken, request } from '../../api/http'
import type { CardStatusKey, CharacterStats, CharacterSummary, PagedResult } from '../../api/types'
import { formatDate } from '../../utils/format'

const filters: { key: string; label: string }[] = [
  { key: 'all', label: '全部' },
  { key: 'draft', label: '草稿' },
  { key: 'ready', label: '待用' },
  { key: 'inUse', label: '使用中' },
  { key: 'retained', label: '留存' },
  { key: 'archived', label: '归档' },
]

const active = ref<string>('all')
const keyword = ref('')
const items = ref<CharacterSummary[]>([])
const stats = ref<CharacterStats | null>(null)
const loading = ref(false)

async function load(): Promise<void> {
  if (!getToken()) {
    uni.reLaunch({ url: '/pages/login/login' })
    return
  }

  loading.value = true
  try {
    const query = `status=${active.value}&keyword=${encodeURIComponent(keyword.value.trim())}&pageSize=100`
    const [page, summary] = await Promise.all([
      request<PagedResult<CharacterSummary>>({ url: `/api/characters?${query}` }),
      request<CharacterStats>({ url: '/api/characters/stats', silent: true }),
    ])
    items.value = page.items
    stats.value = summary
  } catch {
    // 错误提示已统一处理
  } finally {
    loading.value = false
  }
}

function setFilter(key: string): void {
  active.value = key as CardStatusKey | 'all'
  void load()
}

function openDetail(id: string): void {
  uni.navigateTo({ url: `/pages/detail/detail?id=${id}` })
}

function goCreate(): void {
  uni.navigateTo({ url: '/pages/create/create' })
}

onShow(() => {
  void load()
})
</script>

<style lang="scss" scoped>
.hero {
  margin-bottom: 28rpx;
}

.hero-title {
  font-size: 48rpx;
  font-weight: 600;
  letter-spacing: 2rpx;
}

.hero-sub {
  margin-top: 8rpx;
  font-size: 24rpx;
  color: $trpg-muted;
}

.search {
  margin-bottom: 20rpx;
}

.search-input {
  height: 80rpx;
  padding: 0 24rpx;
  border-radius: 16rpx;
  border: 2rpx solid $trpg-line;
  background: #fffdf8;
}

.filters {
  white-space: nowrap;
  margin-bottom: 24rpx;
}

.chip {
  display: inline-block;
  padding: 10rpx 24rpx;
  margin-right: 12rpx;
  border-radius: 999rpx;
  border: 2rpx solid $trpg-line;
  color: $trpg-muted;
  font-size: 24rpx;
  background: #fffdf8;
}

.chip-active {
  background: $trpg-accent;
  border-color: $trpg-accent;
  color: #fffaf0;
}

.card-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.card-name {
  font-size: 34rpx;
  font-weight: 600;
}

.card-meta {
  margin-top: 6rpx;
  font-size: 24rpx;
}

.card-summary {
  margin-top: 14rpx;
  font-size: 26rpx;
  color: #4a4238;
}

.card-foot {
  margin-top: 16rpx;
  font-size: 22rpx;
}

.empty {
  padding: 120rpx 0;
  text-align: center;
}

.empty-title {
  font-size: 34rpx;
  margin-bottom: 12rpx;
}

.empty-text {
  font-size: 24rpx;
  color: $trpg-muted;
}

.fab {
  position: fixed;
  right: 40rpx;
  bottom: 64rpx;
  padding: 22rpx 40rpx;
  border-radius: 999rpx;
  background: $trpg-accent-deep;
  color: #fffaf0;
  font-size: 28rpx;
  box-shadow: 0 12rpx 32rpx rgba(91, 70, 54, 0.28);
}

.fab:active {
  opacity: 0.88;
}
</style>
