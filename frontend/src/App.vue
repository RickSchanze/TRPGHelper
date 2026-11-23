<script setup lang="ts">
import { onLaunch } from '@dcloudio/uni-app'
import { refreshMe } from './stores/session'

onLaunch(() => {
  void refreshMe()
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
</style>
