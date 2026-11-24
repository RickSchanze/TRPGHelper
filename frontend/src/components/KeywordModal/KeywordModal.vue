<template>
  <view v-if="cards.length" class="kw-mask" @tap="handleClose">
    <view class="kw-wrap" @tap.stop>
      <view class="kw-stage">
        <view
          v-for="card in cards"
          :key="card.id"
          class="kw-card"
          :class="{ 'kw-card-back': !card.isTop }"
          :style="cardStyle(card)"
        >
          <view
            class="kw-card-inner"
            :class="{ 'is-top': card.isTop, 'is-closing': closingId === card.id }"
          >
            <view class="kw-head">
              <text class="kw-title">{{ card.title }}</text>
              <text v-if="card.isTop" class="kw-x" @tap.stop="handleClose">×</text>
            </view>
            <text v-if="card.code" class="kw-code">ID {{ card.code }}</text>

            <image
              v-if="card.imageUrl"
              class="kw-image"
              :src="card.imageUrl"
              mode="widthFix"
            />

            <view class="kw-body">
              <KeywordText v-if="card.content" class="kw-content" :text="card.content" />
              <text v-else class="kw-empty">没有找到「{{ card.key }}」的解释。</text>
            </view>

            <view v-if="card.isTop" class="kw-actions">
              <view class="kw-btn" @tap.stop="handleClose">关闭</view>
              <view class="kw-btn kw-btn-primary" @tap.stop="handleCloseAll">关闭全部</view>
            </view>
          </view>
        </view>
      </view>

      <view v-if="stackDepth > 1" class="kw-caption">点「关闭」退回上一个关键词</view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import KeywordText from '../KeywordText/KeywordText.vue'
import {
  closeAllKeywords,
  closeKeyword,
  findKeyword,
  keywordStack,
} from '../../stores/keywords'
import { resolveMediaUrl } from '../../utils/media'

/** 最多同时展示 10 层窗口，再深也只保留最近的 10 个。 */
const MAX_LAYERS = 10

const closingId = ref<number | null>(null)

const stackDepth = computed(() => keywordStack.items.length)

interface CardView {
  id: number
  key: string
  title: string
  code: string
  content: string
  imageUrl: string
  /** 0 = 最前面，数字越大越靠后 */
  depth: number
  isTop: boolean
}

const cards = computed<CardView[]>(() => {
  const items = keywordStack.items.slice(-MAX_LAYERS)
  const last = items.length - 1

  return items.map((item, index) => {
    const entry = findKeyword(item.key)
    const depth = last - index

    return {
      id: item.id,
      key: item.key,
      title: entry?.title || item.key,
      code: entry?.code || '',
      content: entry?.content || '',
      imageUrl: resolveMediaUrl(entry?.imageUrl),
      depth,
      isTop: depth === 0,
    }
  })
})

/* 叠放偏移：桌面大一点、手机上小一点；到上限后不再移动（原地弹）。 */
const screenWidth = uni.getSystemInfoSync().windowWidth || 375
const isDesktop = screenWidth >= 900
const stepX = isDesktop ? 14 : 8
const stepY = isDesktop ? 18 : 14
const capX = isDesktop ? 70 : 24
const capY = isDesktop ? 90 : 56

function cardStyle(card: CardView): Record<string, string> {
  const offsetX = Math.min(card.depth * stepX, capX)
  const offsetY = Math.min(card.depth * stepY, capY)

  return {
    position: card.isTop ? 'relative' : 'absolute',
    top: '0',
    right: '0',
    bottom: card.isTop ? 'auto' : '0',
    left: '0',
    zIndex: String(60 - card.depth),
    opacity: card.isTop ? '1' : String(Math.max(0.42, 0.92 - card.depth * 0.05)),
    transform: `translate(${-offsetX}px, ${-offsetY}px)`,
  }
}

function finishClose(action: () => void): void {
  const top = cards.value.find((card) => card.isTop)
  if (!top) {
    action()
    return
  }

  closingId.value = top.id
  setTimeout(() => {
    closingId.value = null
    action()
  }, 150)
}

function handleClose(): void {
  if (closingId.value !== null) {
    return
  }

  finishClose(closeKeyword)
}

function handleCloseAll(): void {
  if (closingId.value !== null) {
    return
  }

  finishClose(closeAllKeywords)
}
</script>

<style lang="scss" scoped>
.kw-mask {
  position: fixed;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  z-index: 999;
  display: flex;
  align-items: center;
  justify-content: center;
  box-sizing: border-box;
  padding: 20px;
  background: rgba(30, 23, 18, 0.55);
  animation: kw-fade 0.16s ease-out;
}

.kw-wrap {
  width: 100%;
  max-width: 560px;
}

.kw-stage {
  position: relative;
}

/* 每一层窗口：外框只负责定位与移动，内层负责外观与动画 */
.kw-card {
  box-sizing: border-box;
  width: 100%;
  transition: transform 0.24s cubic-bezier(0.22, 0.8, 0.3, 1), opacity 0.24s ease;
  will-change: transform, opacity;
}

.kw-card-back {
  pointer-events: none;
}

.kw-card-inner {
  display: flex;
  flex-direction: column;
  box-sizing: border-box;
  width: 100%;
  max-height: 78vh;
  overflow: hidden;
  border: 1px solid #d8c6a5;
  border-radius: 18px;
  background: #fffdf8;
  box-shadow: 0 24px 60px rgba(30, 23, 18, 0.35);
}

.kw-card-back .kw-card-inner {
  height: 100%;
  box-shadow: 0 10px 26px rgba(30, 23, 18, 0.18);
}

/* 新的窗口从后面「弹」到前面 */
.kw-card-inner.is-top {
  animation: kw-pop-in 0.26s cubic-bezier(0.2, 0.9, 0.3, 1.15);
}

.kw-card-inner.is-closing {
  animation: kw-pop-out 0.15s ease forwards;
}

.kw-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px 20px 12px;
  border-bottom: 1px solid #f0e7d9;
}

.kw-title {
  min-width: 0;
  font-size: 19px;
  font-weight: 600;
  letter-spacing: 1px;
  word-break: break-word;
}

.kw-code {
  display: block;
  margin-top: -2px;
  font-size: 11px;
  letter-spacing: 1px;
  color: #a08a6c;
  font-variant-numeric: tabular-nums;
}

.kw-x {
  padding: 0 4px;
  font-size: 24px;
  line-height: 1;
  color: #a08f79;
}

.kw-image {
  display: block;
  width: 100%;
  background: #2b211a;
}

.kw-body {
  box-sizing: border-box;
  width: 100%;
  padding: 16px 20px;
  overflow-x: hidden;
  overflow-y: auto;
}

.kw-content {
  display: block;
  box-sizing: border-box;
  width: 100%;
  font-size: 15px;
  line-height: 1.75;
  color: #4a4238;
  white-space: normal;
  word-break: break-word;
  overflow-wrap: anywhere;
}

.kw-empty {
  font-size: 14px;
  color: #a08f79;
}

.kw-actions {
  display: flex;
  gap: 12px;
  padding: 12px 20px 18px;
}

.kw-btn {
  flex: 1;
  padding: 10px 0;
  border: 1px solid #d8c6a5;
  border-radius: 999px;
  font-size: 14px;
  color: #6b5c49;
  text-align: center;
}

.kw-btn-primary {
  border-color: #8a5a2b;
  background: #8a5a2b;
  color: #fffaf0;
}

.kw-btn:active {
  opacity: 0.85;
}

.kw-caption {
  margin-top: 14px;
  font-size: 12px;
  color: rgba(255, 250, 240, 0.85);
  text-align: center;
}

@keyframes kw-pop-in {
  from {
    opacity: 0.4;
    transform: scale(0.92);
  }

  to {
    opacity: 1;
    transform: scale(1);
  }
}

@keyframes kw-pop-out {
  to {
    opacity: 0;
    transform: scale(0.96);
  }
}

@keyframes kw-fade {
  from {
    opacity: 0;
  }

  to {
    opacity: 1;
  }
}
</style>
