<template>
  <view class="app-header">
    <view class="header-inner">
      <view class="brand" @tap="goIndex">
        <text class="brand-mark">TRPG</text>
        <text class="brand-name">卡器</text>
      </view>

      <view class="nav">
        <text class="nav-item" :class="{ 'nav-active': current === 'index' }" @tap="goIndex">卡库</text>
        <text class="nav-item" :class="{ 'nav-active': current === 'mine' }" @tap="goMine">我的</text>
        <text class="nav-create" @tap="goCreate">＋ 车新卡</text>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
defineProps<{ current?: 'index' | 'mine' | 'create' | 'detail' }>()

import { clearDraft } from '../../stores/createDraft'

function goIndex(): void {
  uni.switchTab({ url: '/pages/index/index' })
}

function goMine(): void {
  uni.switchTab({ url: '/pages/mine/mine' })
}

function goCreate(): void {
  // 每次点「车新卡」都从空白开始；同一次会话里靠本机草稿继续保留
  clearDraft()
  uni.navigateTo({ url: '/pages/create/create' })
}
</script>

<style lang="scss">
.app-header {
  display: none;
}

@media (min-width: 900px) {
  .app-header {
    display: block;
    position: fixed;
    top: 0;
    left: 0;
    right: 0;
    z-index: 90;
    background: rgba(246, 241, 231, 0.94);
    backdrop-filter: blur(10px);
    border-bottom: 1px solid $trpg-line;
  }

  .header-inner {
    max-width: 1100px;
    margin: 0 auto;
    height: 72px;
    padding: 0 40px;
    display: flex;
    align-items: center;
    justify-content: space-between;
  }

  .brand {
    display: flex;
    align-items: baseline;
    gap: 8px;
    cursor: pointer;
  }

  .brand-mark {
    font-size: 13px;
    letter-spacing: 4px;
    color: $trpg-muted;
  }

  .brand-name {
    font-size: 22px;
    font-weight: 600;
    letter-spacing: 2px;
    color: $trpg-ink;
  }

  .nav {
    display: flex;
    align-items: center;
    gap: 28px;
  }

  .nav-item {
    font-size: 15px;
    color: $trpg-muted;
    cursor: pointer;
    padding: 6px 0;
    border-bottom: 2px solid transparent;
  }

  .nav-item:hover {
    color: $trpg-ink;
  }

  .nav-active {
    color: $trpg-ink;
    border-bottom-color: $trpg-accent;
  }

  .nav-create {
    font-size: 14px;
    padding: 8px 20px;
    border-radius: 999px;
    background: $trpg-accent-deep;
    color: #fffaf0;
    cursor: pointer;
  }

  .nav-create:hover {
    opacity: 0.9;
  }
}
</style>
