<template>
  <view class="page">
    <view class="profile">
      <view class="avatar">{{ initial }}</view>
      <view class="profile-main">
        <view class="nickname">{{ session.user?.nickName || '未登录' }}</view>
        <view class="muted sub">{{ session.user ? '本地账号' : '登录后同步你的卡库' }}</view>
      </view>
    </view>

    <view v-if="session.user" class="stats">
      <view class="stat">
        <view class="stat-value">{{ session.user.totalCards }}</view>
        <view class="stat-label muted">已车</view>
      </view>
      <view class="stat">
        <view class="stat-value">{{ session.user.retainedCards }}</view>
        <view class="stat-label muted">留存</view>
      </view>
      <view class="stat">
        <view class="stat-value">{{ stats?.inUse ?? 0 }}</view>
        <view class="stat-label muted">使用中</view>
      </view>
    </view>

    <view class="section">
      <view class="row" @tap="refresh">
        <text>刷新资料</text>
        <text class="muted">↻</text>
      </view>
      <view class="row" @tap="goIndex">
        <text>回卡库</text>
        <text class="muted">›</text>
      </view>
    </view>

    <button v-if="session.user" class="btn btn-plain" @tap="signOut">退出登录</button>
    <button v-else class="btn" @tap="goLogin">去登录</button>

    <view class="footer muted">TRPG 卡器 v0.1.0 · 骨架版</view>
  </view>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { getToken, request } from '../../api/http'
import { goLogin, logout, refreshMe, session } from '../../stores/session'
import type { CharacterStats } from '../../api/types'

const stats = ref<CharacterStats | null>(null)

const initial = computed(() => {
  const nickName = session.user?.nickName || '?'
  return nickName.slice(0, 1)
})

async function refresh(): Promise<void> {
  const user = await refreshMe()
  if (!user) {
    return
  }

  try {
    stats.value = await request<CharacterStats>({ url: '/api/characters/stats', silent: true })
  } catch {
    // ignore
  }
}

function signOut(): void {
  logout()
  stats.value = null
  uni.reLaunch({ url: '/pages/login/login' })
}

function goIndex(): void {
  uni.switchTab({ url: '/pages/index/index' })
}

onShow(() => {
  if (getToken()) {
    void refresh()
  }
})
</script>

<style lang="scss" scoped>
.profile {
  display: flex;
  align-items: center;
  padding: 20rpx 0 36rpx;
}

.avatar {
  width: 112rpx;
  height: 112rpx;
  border-radius: 50%;
  background: $trpg-accent;
  color: #fffaf0;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 44rpx;
  margin-right: 24rpx;
}

.nickname {
  font-size: 38rpx;
  font-weight: 600;
}

.sub {
  font-size: 24rpx;
  margin-top: 6rpx;
}

.stats {
  display: flex;
  background: #fffdf8;
  border: 2rpx solid $trpg-line;
  border-radius: 20rpx;
  padding: 28rpx 0;
  margin-bottom: 24rpx;
}

.stat {
  flex: 1;
  text-align: center;
}

.stat-value {
  font-size: 44rpx;
  font-weight: 600;
  color: $trpg-accent-deep;
}

.stat-label {
  font-size: 22rpx;
}

.section {
  background: #fffdf8;
  border: 2rpx solid $trpg-line;
  border-radius: 20rpx;
  padding: 0 28rpx;
  margin-bottom: 32rpx;
}

.row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 96rpx;
  border-bottom: 2rpx solid $trpg-line;
}

.row:last-child {
  border-bottom: none;
}

.footer {
  margin-top: 48rpx;
  text-align: center;
  font-size: 22rpx;
}
</style>
