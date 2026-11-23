<template>
  <view class="page login-page">
    <view class="brand">
      <view class="brand-mark">TRPG</view>
      <view class="brand-title">电子卡器</view>
      <view class="brand-sub">记录你车过的每一张卡，留住想留的那几张。</view>
    </view>

    <view class="panel">
      <button class="btn" :disabled="loading" @tap="loginWithWeChat">微信一键登录</button>

      <view class="divider"><text class="muted">或</text></view>

      <input v-model="nickName" class="input" placeholder="给自己起个昵称" maxlength="32" />
      <button class="btn btn-plain" :disabled="loading" @tap="loginWithDev">本地开发登录</button>

      <view class="hint muted">开发登录只在 Development 环境可用（配置项 Auth:EnableDevLogin）。</view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { request } from '../../api/http'
import { applyLogin } from '../../stores/session'
import type { LoginResponse } from '../../api/types'

const nickName = ref('')
const loading = ref(false)

async function doLogin(url: string, data: Record<string, unknown>): Promise<void> {
  loading.value = true
  try {
    const result = await request<LoginResponse>({ url, method: 'POST', data, auth: false })
    applyLogin(result)
    uni.switchTab({ url: '/pages/index/index' })
  } catch {
    // 错误提示已由 request 统一弹出
  } finally {
    loading.value = false
  }
}

function loginWithDev(): void {
  void doLogin('/api/auth/dev-login', { nickName: nickName.value.trim() || undefined })
}

function loginWithWeChat(): void {
  // #ifdef MP-WEIXIN
  uni.login({
    provider: 'weixin',
    success: (result) => {
      if (!result.code) {
        uni.showToast({ title: '没有拿到微信 code', icon: 'none' })
        return
      }
      void doLogin('/api/auth/wechat/login', { code: result.code })
    },
    fail: () => {
      uni.showToast({ title: '微信登录失败', icon: 'none' })
    },
  })
  // #endif

  // #ifndef MP-WEIXIN
  uni.showToast({ title: '微信登录请在微信小程序里使用', icon: 'none' })
  // #endif
}
</script>

<style lang="scss" scoped>
.login-page {
  display: flex;
  flex-direction: column;
  justify-content: center;
  padding-bottom: 120rpx;
}

.brand {
  text-align: center;
  margin-bottom: 72rpx;
}

.brand-mark {
  font-size: 32rpx;
  letter-spacing: 12rpx;
  color: $trpg-accent;
  margin-bottom: 16rpx;
}

.brand-title {
  font-size: 64rpx;
  font-weight: 600;
  letter-spacing: 6rpx;
}

.brand-sub {
  margin-top: 20rpx;
  font-size: 26rpx;
  color: $trpg-muted;
}

.panel {
  background: #fffdf8;
  border: 2rpx solid $trpg-line;
  border-radius: 24rpx;
  padding: 40rpx 32rpx;
}

.input {
  height: 88rpx;
  border: 2rpx solid $trpg-line;
  border-radius: 16rpx;
  padding: 0 24rpx;
  margin-bottom: 24rpx;
  background: #fff;
}

.divider {
  display: flex;
  align-items: center;
  margin: 28rpx 0;
}

.divider::before,
.divider::after {
  content: '';
  flex: 1;
  height: 2rpx;
  background: $trpg-line;
}

.divider text {
  margin: 0 20rpx;
  font-size: 24rpx;
}

.hint {
  margin-top: 24rpx;
  font-size: 22rpx;
  text-align: center;
}
</style>
