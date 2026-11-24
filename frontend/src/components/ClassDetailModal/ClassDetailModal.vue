<template>
  <view v-if="visible" class="cd-mask" @tap="close">
    <view class="cd-card" @tap.stop>
      <view class="cd-cover">
        <image
          v-if="coverUrl && !coverFailed"
          class="cd-cover-image"
          :src="coverUrl"
          mode="aspectFill"
          @error="coverFailed = true"
        />
        <view v-else class="cd-cover-fallback">
          <text>{{ (option?.name || '职')[0] }}</text>
        </view>
        <view class="cd-cover-shade" />

        <view class="cd-cover-text">
          <view class="cd-name">
            <KeywordText :text="option?.name" />
          </view>
          <view class="cd-meta">
            <text>生命骰 </text>
            <KeywordText :text="option?.hitDie || '-'" />
            <text> · 主要属性 </text>
            <KeywordText :text="option?.primaryAbility || '-'" />
          </view>
        </view>

        <text class="cd-x" @tap.stop="close">×</text>
      </view>

      <scroll-view scroll-y class="cd-body" style="max-height: 56vh">
        <view v-if="option?.description" class="cd-desc">
          <KeywordText :text="option.description" />
        </view>

        <view v-if="loading" class="cd-loading muted">正在加载等级成长…</view>

        <view v-else-if="error" class="cd-error" @tap="load">
          <text class="muted">加载失败，点这里重试</text>
        </view>

        <view v-else class="cd-levels">
          <view v-for="row in levelRows" :key="row.level" class="cd-level">
            <view class="cd-level-badge">Lv {{ row.level }}</view>
            <view class="cd-level-items">
              <view v-for="(feature, index) in row.features" :key="`${row.level}-${index}`" class="cd-feature">
                <view class="cd-feature-title">
                  <KeywordText :text="feature.title" />
                </view>
                <view class="cd-feature-desc">
                  <KeywordText :text="feature.description" />
                </view>
              </view>
              <view v-if="!row.features.length" class="cd-feature-desc muted">—</view>
            </view>
          </view>
        </view>

        <view class="cd-note muted">子职（武术范型 / 游侠范型 / 宗派 / 结社 / 神圣誓约）暂未接入。</view>
      </scroll-view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { request } from '../../api/http'
import { resolveMediaUrl } from '../../utils/media'
import { ensureKeywords } from '../../stores/keywords'
import KeywordText from '../KeywordText/KeywordText.vue'
import type { ClassLevels, ClassOption } from '../../api/types'

const props = defineProps<{
  visible: boolean
  option: ClassOption | null
  systemCode?: string | null
}>()

const emit = defineEmits<{ (event: 'close'): void }>()

const loading = ref(false)
const error = ref(false)
const coverFailed = ref(false)
const levels = ref<ClassLevels | null>(null)

const coverUrl = computed(() => (coverFailed.value ? '' : resolveMediaUrl(props.option?.imageUrl)))

interface LevelRow {
  level: number
  features: Array<{ title: string; description: string }>
}

const levelRows = computed<LevelRow[]>(() => {
  const features = levels.value?.features ?? []

  return Array.from({ length: 20 }, (_, index) => {
    const level = index + 1
    return {
      level,
      features: features
        .filter((feature) => feature.level === level)
        .map((feature) => ({ title: feature.title, description: feature.description })),
    }
  })
})

async function load(): Promise<void> {
  const code = props.option?.code
  if (!code) {
    return
  }

  loading.value = true
  error.value = false

  try {
    const systemCode = props.systemCode || 'dnd5e'
    levels.value = await request<ClassLevels>({
      url: `/api/classes/${encodeURIComponent(code)}/levels?systemCode=${encodeURIComponent(systemCode)}`,
      auth: false,
    })
  } catch {
    error.value = true
  } finally {
    loading.value = false
  }
}

function close(): void {
  emit('close')
}

watch(
  () => props.visible,
  (visible) => {
    if (!visible) {
      return
    }

    coverFailed.value = false
    levels.value = null
    // 打开职业详情时强制刷新一次词条，避免刚在管理台加的关键词点不开
    void ensureKeywords(true)
    void load()
  },
)
</script>

<style lang="scss" scoped>
.cd-mask {
  position: fixed;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  z-index: 990;
  display: flex;
  align-items: center;
  justify-content: center;
  box-sizing: border-box;
  padding: 20px;
  background: rgba(30, 23, 18, 0.55);
  animation: cd-fade 0.16s ease-out;
}

.cd-card {
  display: flex;
  flex-direction: column;
  box-sizing: border-box;
  width: 100%;
  max-width: 620px;
  max-height: 88vh;
  overflow: hidden;
  border: 1px solid #d8c6a5;
  border-radius: 18px;
  background: #fffdf8;
  box-shadow: 0 24px 60px rgba(30, 23, 18, 0.35);
  animation: cd-in 0.24s cubic-bezier(0.2, 0.8, 0.3, 1);
}

.cd-cover {
  position: relative;
  height: 150px;
  background: #2b211a;
}

.cd-cover-image {
  display: block;
  width: 100%;
  height: 100%;
}

.cd-cover-fallback {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 100%;
  height: 100%;
  font-size: 48px;
  color: #fffaf0;
}

.cd-cover-shade {
  position: absolute;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  background: linear-gradient(180deg, rgba(20, 15, 11, 0.1) 0%, rgba(20, 15, 11, 0.78) 100%);
}

.cd-cover-text {
  position: absolute;
  right: 20px;
  bottom: 14px;
  left: 20px;
  color: #fffaf0;
}

.cd-name {
  font-size: 24px;
  font-weight: 600;
  letter-spacing: 2px;
}

.cd-meta {
  margin-top: 4px;
  font-size: 12px;
  color: rgba(255, 250, 240, 0.82);
}

.cd-x {
  position: absolute;
  top: 10px;
  right: 14px;
  font-size: 26px;
  line-height: 1;
  color: rgba(255, 250, 240, 0.9);
}

.cd-body {
  box-sizing: border-box;
  padding: 18px 20px 20px;
}

.cd-desc {
  margin-bottom: 14px;
  font-size: 14px;
  line-height: 1.7;
  color: #4a4238;
}

.cd-loading,
.cd-error {
  padding: 18px 0;
}

.cd-levels {
  border-top: 1px solid #f0e7d9;
}

.cd-level {
  display: flex;
  gap: 14px;
  padding: 12px 0;
  border-bottom: 1px dashed #efe4d3;
}

.cd-level-badge {
  flex-shrink: 0;
  width: 52px;
  height: 24px;
  border-radius: 999px;
  background: #f3e7d4;
  color: #8a5a2b;
  font-size: 12px;
  line-height: 24px;
  text-align: center;
}

.cd-level-items {
  flex: 1;
  min-width: 0;
}

.cd-feature + .cd-feature {
  margin-top: 8px;
}

.cd-feature-title {
  font-size: 14px;
  font-weight: 600;
  color: #2b211a;
}

.cd-feature-desc {
  margin-top: 2px;
  font-size: 13px;
  line-height: 1.65;
  color: #6b5c49;
}

.cd-note {
  margin-top: 14px;
  font-size: 12px;
  text-align: center;
}

@keyframes cd-in {
  from {
    opacity: 0;
    transform: translateY(14px) scale(0.97);
  }

  to {
    opacity: 1;
    transform: translateY(0) scale(1);
  }
}

@keyframes cd-fade {
  from {
    opacity: 0;
  }

  to {
    opacity: 1;
  }
}

@media (max-width: 900px) {
  .cd-cover {
    height: 120px;
  }

  .cd-name {
    font-size: 20px;
  }
}
</style>
