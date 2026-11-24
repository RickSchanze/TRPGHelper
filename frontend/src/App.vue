<script setup lang="ts">
import { onLaunch } from '@dcloudio/uni-app'
import { refreshMe } from './stores/session'
import { ensureKeywords } from './stores/keywords'

onLaunch(() => {
  void refreshMe()
  void ensureKeywords()
})
</script>

<style lang="scss">
page {
  background: $trpg-bg;
  color: $trpg-ink;
  font-size: 28rpx;
  line-height: 1.6;
}

.page {
  min-height: 100vh;
  padding: 32rpx 32rpx 160rpx;
  box-sizing: border-box;
}

.card {
  background: #fffdf8;
  border: 2rpx solid $trpg-line;
  border-radius: 20rpx;
  padding: 28rpx;
  margin-bottom: 24rpx;
}

.badge {
  font-size: 22rpx;
  padding: 4rpx 16rpx;
  border-radius: 999rpx;
  line-height: 1.4;
}

.badge-draft { background: #eee7da; color: #8a7c68; }
.badge-ready { background: #e3efe4; color: #3f7a4c; }
.badge-inUse { background: #e2ecf7; color: #3a6a9b; }
.badge-retained { background: #f7ecd9; color: #8a6420; }
.badge-archived { background: #e9e6e0; color: #7b746a; }

.btn {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 88rpx;
  border-radius: 999rpx;
  font-size: 30rpx;
  background: $trpg-accent;
  color: #fffaf0;
}

.btn-plain {
  background: transparent;
  border: 2rpx solid $trpg-accent;
  color: $trpg-accent;
}

.btn:active {
  opacity: 0.85;
}

.muted {
  color: $trpg-muted;
}

/* ---------- 桌面浏览器（H5）适配 ---------- */
@media (min-width: 900px) {
  /* 桌面用固定顶栏，隐藏小程序式导航条和底部 tabBar */
  uni-page-head {
    display: none !important;
  }

  uni-tabbar {
    display: none !important;
  }

  page {
    font-size: 15px;
  }

  .page {
    max-width: 1100px;
    margin: 0 auto;
    padding: 112px 40px 80px;
  }

  .card {
    border-width: 1px;
    border-radius: 14px;
    padding: 24px;
    margin-bottom: 20px;
    transition: transform 0.15s ease, box-shadow 0.15s ease;
  }

  .card:hover {
    transform: translateY(-2px);
    box-shadow: 0 10px 28px rgba(91, 70, 54, 0.1);
  }

  .btn {
    height: 48px;
    font-size: 15px;
    border-radius: 999px;
  }

  button.btn {
    border: none;
  }

  .btn:hover {
    opacity: 0.92;
  }

  .badge {
    font-size: 12px;
    padding: 2px 12px;
    border-radius: 999px;
  }
}
</style>
