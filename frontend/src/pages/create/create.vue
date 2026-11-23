<template>
  <view class="page wizard-page">
    <AppHeader current="create" />

    <view class="hero">
      <view class="hero-en">WHO ARE YOU</view>
      <view class="hero-zh">你是谁</view>
    </view>

    <view class="stepper">
      <view
        v-for="(label, index) in stepLabels"
        :key="label"
        class="step"
        :class="{ 'step-active': step === index + 1, 'step-done': step > index + 1 }"
      >
        <view class="step-dot">{{ step > index + 1 ? '✓' : index + 1 }}</view>
        <text class="step-label">{{ label }}</text>
      </view>
    </view>

    <!-- 第一步：规则书 -->
    <view v-if="step === 1" class="panel">
      <view class="panel-title">选择规则书</view>

      <view class="system-grid">
        <view
          v-for="system in systems"
          :key="system.code"
          class="system-card"
          :class="{
            'system-selected': draft.systemCode === system.code,
            'system-locked': system.code !== 'dnd5e',
          }"
          @tap="pickSystem(system)"
        >
          <view class="system-icon-wrap" :style="{ backgroundColor: artworkFor(system.code).accent }">
            <image
              v-if="!failedIcons[system.code]"
              class="system-icon"
              :src="artworkFor(system.code).icon"
              mode="aspectFit"
              @error="markIconFailed(system.code)"
            />
            <text v-else class="system-icon-fallback">{{ system.name.slice(0, 1) }}</text>
          </view>

          <view class="system-info">
            <view class="system-name">{{ system.name }}</view>
            <view class="system-desc muted">{{ system.description }}</view>
          </view>

          <text v-if="system.code !== 'dnd5e'" class="system-lock muted">即将开放</text>
          <view v-else-if="draft.systemCode === system.code" class="system-check">✓</view>
        </view>
      </view>
    </view>

    <!-- 第二步：职业 -->
    <view v-else class="panel">
      <view class="panel-title">选择职业</view>

      <view v-if="classesLoading" class="muted">正在加载职业…</view>
      <view v-else-if="classesError" class="load-error" @tap="loadClasses">
        <text class="muted">职业加载失败，点这里重试</text>
      </view>

      <view v-else class="class-list">
        <view
          v-for="item in classes"
          :key="item.code"
          class="class-card"
          :class="{ 'class-selected': draft.classCode === item.code }"
          @tap="pickClass(item)"
        >
          <view class="class-cover-wrap">
            <image
              v-if="!failedCovers[item.code]"
              class="class-cover"
              :src="classArtworkFor(item.code).image"
              mode="aspectFit"
              @error="failedCovers[item.code] = true"
            />
            <view
              v-else
              class="class-cover class-cover-fallback"
              :style="{ backgroundColor: classArtworkFor(item.code).accent }"
            >
              <text>{{ item.name.slice(0, 1) }}</text>
            </view>
            <view v-if="draft.classCode === item.code" class="class-check">✓</view>
          </view>

          <view class="class-body">
            <view class="class-name">{{ item.name }}</view>
            <view class="class-sub">生命骰 {{ item.hitDie }} · 主要属性 {{ item.primaryAbility }}</view>
            <view class="class-desc">{{ item.description }}</view>
            <view class="feature-list">
              <view v-for="feature in item.features" :key="feature" class="feature">
                <text class="feature-dot">◆</text>
                <text class="feature-text">{{ feature }}</text>
              </view>
            </view>
          </view>
        </view>
      </view>
    </view>

    <view class="wizard-actions">
      <button v-if="step > 1" class="btn btn-plain" @tap="prev">上一步</button>
      <button v-if="step === 1" class="btn" :disabled="!draft.systemCode" @tap="next">下一步</button>
      <button v-else class="btn" :disabled="!draft.classCode || finishing" @tap="finish">下一步</button>
    </view>
  </view>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { onHide, onLoad, onUnload } from '@dcloudio/uni-app'
import { request } from '../../api/http'
import { classArtworkFor } from '../../config/classes'
import { artworkFor } from '../../config/rulebooks'
import type { ClassOption, GameSystem } from '../../api/types'
import {
  clearDraft,
  emptyDraft,
  loadDraft,
  saveDraft,
  type CreateDraft,
  type CreateStep,
} from '../../stores/createDraft'
import AppHeader from '../../components/AppHeader/AppHeader.vue'

const stepLabels = ['规则书', '职业']

const draft = ref<CreateDraft>(emptyDraft())
const systems = ref<GameSystem[]>([])
const classes = ref<ClassOption[]>([])
const classesLoading = ref(false)
const classesError = ref(false)
const ready = ref(false)
const finishing = ref(false)
const failedIcons = ref<Record<string, boolean>>({})
const failedCovers = ref<Record<string, boolean>>({})

const step = computed(() => draft.value.step)

let saveTimer: ReturnType<typeof setTimeout> | null = null

function scheduleSave(): void {
  if (!ready.value) {
    return
  }

  if (saveTimer) {
    clearTimeout(saveTimer)
  }

  saveTimer = setTimeout(() => {
    saveDraft(draft.value)
  }, 250)
}

watch(draft, scheduleSave, { deep: true })

onHide(() => {
  if (ready.value) {
    saveDraft(draft.value)
  }
})

onUnload(() => {
  if (ready.value) {
    saveDraft(draft.value)
  }
})

onLoad(() => {
  void init()
})

async function init(): Promise<void> {
  const restored = loadDraft()
  if (restored) {
    draft.value = restored
  }

  await loadSystems()

  if (!draft.value.systemCode || draft.value.systemCode !== 'dnd5e') {
    draft.value.systemCode = null
    draft.value.step = 1
    draft.value.classCode = null
    draft.value.className = ''
  }

  if (draft.value.systemCode) {
    await loadClasses()
  }

  ready.value = true
}

async function loadSystems(): Promise<void> {
  try {
    systems.value = await request<GameSystem[]>({ url: '/api/systems', auth: false })
  } catch {
    // 错误提示已统一处理
  }
}

async function loadClasses(): Promise<void> {
  classesLoading.value = true
  classesError.value = false

  try {
    const code = draft.value.systemCode ?? 'dnd5e'
    classes.value = await request<ClassOption[]>({
      url: `/api/generator/classes?systemCode=${code}`,
      auth: false,
    })
  } catch {
    classesError.value = true
  } finally {
    classesLoading.value = false
  }
}

function markIconFailed(code: string): void {
  failedIcons.value[code] = true
}

function pickSystem(system: GameSystem): void {
  if (system.code !== 'dnd5e') {
    return
  }

  if (draft.value.systemCode === system.code) {
    return
  }

  draft.value.systemCode = system.code
  draft.value.classCode = null
  draft.value.className = ''
  classes.value = []
  void loadClasses()
}

function pickClass(option: ClassOption): void {
  draft.value.classCode = option.code
  draft.value.className = option.name
}

function next(): void {
  if (step.value === 1 && !draft.value.systemCode) {
    toast('先选规则书')
    return
  }

  if (step.value === 1 && classes.value.length === 0) {
    void loadClasses()
  }

  draft.value.step = (step.value + 1) as CreateStep
  saveDraft(draft.value)
  scrollTop()
}

function prev(): void {
  if (step.value <= 1) {
    return
  }

  draft.value.step = (step.value - 1) as CreateStep
  scrollTop()
}

async function finish(): Promise<void> {
  if (!draft.value.classCode) {
    toast('先选一个职业')
    return
  }

  finishing.value = true
  try {
    let name = ''
    try {
      const names = await request<string[]>({
        url: `/api/generator/names?systemCode=${draft.value.systemCode}&count=1`,
        auth: false,
      })
      name = names[0] ?? ''
    } catch {
      // 名字生成失败就用占位名
    }

    const created = await request<{ id: string }>({
      url: '/api/characters',
      method: 'POST',
      data: {
        systemCode: draft.value.systemCode,
        name: name || '未命名冒险者',
        className: draft.value.className,
      },
    })

    clearDraft()
    uni.redirectTo({ url: `/pages/detail/detail?id=${created.id}` })
  } catch {
    // 提交失败时保留本地草稿
  } finally {
    finishing.value = false
  }
}

function toast(title: string): void {
  uni.showToast({ title, icon: 'none' })
}

function scrollTop(): void {
  uni.pageScrollTo({ scrollTop: 0, duration: 200 })
}
</script>

<style lang="scss" scoped>
.hero {
  padding: 8rpx 0 32rpx;
  text-align: center;
}

.hero-en {
  font-family: 'Songti SC', 'SimSun', 'STSong', 'Noto Serif SC', serif;
  font-size: 64rpx;
  font-weight: 600;
  letter-spacing: 8rpx;
  line-height: 1.2;
}

.hero-zh {
  margin-top: 8rpx;
  font-family: 'Songti SC', 'SimSun', 'STSong', 'Noto Serif SC', serif;
  font-size: 24rpx;
  letter-spacing: 8rpx;
  color: $trpg-muted;
}

.stepper {
  display: flex;
  margin-bottom: 28rpx;
}

.step {
  position: relative;
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.step::after {
  content: '';
  position: absolute;
  top: 26rpx;
  left: 50%;
  z-index: 0;
  width: 100%;
  height: 2rpx;
  background: $trpg-line;
}

.step:last-child::after {
  display: none;
}

.step-dot {
  position: relative;
  z-index: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 52rpx;
  height: 52rpx;
  border-radius: 50%;
  border: 2rpx solid $trpg-line;
  background: #fffdf8;
  color: $trpg-muted;
  font-size: 24rpx;
}

.step-done .step-dot {
  border-color: #d8c6a5;
  background: #e7dbc4;
  color: $trpg-accent-deep;
}

.step-active .step-dot {
  border-color: $trpg-accent;
  background: $trpg-accent;
  color: #fffaf0;
}

.step-label {
  margin-top: 8rpx;
  font-size: 20rpx;
  color: $trpg-muted;
}

.step-active .step-label {
  color: $trpg-ink;
  font-weight: 600;
}

.panel {
  padding: 28rpx;
  margin-bottom: 24rpx;
  border: 2rpx solid $trpg-line;
  border-radius: 20rpx;
  background: #fffdf8;
}

.panel-title {
  margin-bottom: 20rpx;
  font-size: 30rpx;
  font-weight: 600;
}

.system-grid {
  display: flex;
  flex-direction: column;
  gap: 20rpx;
}

.system-card {
  position: relative;
  display: flex;
  gap: 20rpx;
  padding: 20rpx;
  border: 2rpx solid $trpg-line;
  border-radius: 16rpx;
  background: #fff;
}

.system-selected {
  border-color: $trpg-accent;
  box-shadow: 0 8rpx 24rpx rgba(91, 70, 54, 0.12);
}

.system-locked {
  opacity: 0.55;
}

.system-icon-wrap {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 112rpx;
  height: 112rpx;
  border-radius: 14rpx;
  flex-shrink: 0;
}

.system-icon {
  width: 84rpx;
  height: 84rpx;
}

.system-icon-fallback {
  color: #fffaf0;
  font-size: 48rpx;
}

.system-info {
  flex: 1;
  min-width: 0;
}

.system-name {
  font-size: 28rpx;
  font-weight: 600;
}

.system-desc {
  margin-top: 6rpx;
  font-size: 22rpx;
}

.system-lock {
  position: absolute;
  top: 16rpx;
  right: 16rpx;
  font-size: 20rpx;
}

.system-check {
  position: absolute;
  top: 16rpx;
  right: 16rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 40rpx;
  height: 40rpx;
  border-radius: 50%;
  background: $trpg-accent;
  color: #fffaf0;
  font-size: 22rpx;
}

.load-error {
  padding: 20rpx 0;
}

.class-list {
  display: flex;
  flex-direction: column;
  gap: 24rpx;
}

.class-card {
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
  border: 2rpx solid $trpg-line;
  border-radius: 20rpx;
  background: #fff;
}

.class-selected {
  border-color: $trpg-accent;
  box-shadow: 0 10rpx 30rpx rgba(91, 70, 54, 0.16);
}

.class-cover-wrap {
  position: relative;
  width: 100%;
  height: 0;
  padding-top: 56.25%;
  overflow: hidden;
  background: #2b211a;
}

.class-cover {
  position: absolute;
  top: 0;
  right: 0;
  bottom: 0;
  left: 0;
  display: block;
  width: 100%;
  height: 100%;
}

.class-cover-fallback {
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fffaf0;
  font-size: 64rpx;
}

.class-name {
  font-size: 30rpx;
  font-weight: 600;
  letter-spacing: 2rpx;
}

.class-sub {
  margin-top: 4rpx;
  font-size: 20rpx;
  color: $trpg-muted;
}

.class-check {
  position: absolute;
  top: 20rpx;
  right: 20rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  width: 48rpx;
  height: 48rpx;
  border-radius: 50%;
  background: $trpg-accent;
  color: #fffaf0;
  font-size: 26rpx;
}

.class-body {
  flex: 1;
  padding: 20rpx;
}

.class-desc {
  margin-top: 12rpx;
  font-size: 22rpx;
  line-height: 1.5;
  color: #4a4238;
}

.feature-list {
  display: grid;
  grid-template-columns: 1fr 1fr;
  column-gap: 24rpx;
  row-gap: 8rpx;
  margin-top: 12rpx;
}

.feature {
  display: flex;
  gap: 8rpx;
  font-size: 20rpx;
  line-height: 1.5;
  color: $trpg-muted;
}

.feature-dot {
  color: $trpg-accent;
}

.btn[disabled] {
  opacity: 0.4;
}

.wizard-actions {
  display: flex;
  gap: 20rpx;
}

.wizard-actions .btn {
  flex: 1;
}

@media (min-width: 900px) {
  .page {
    max-width: 860px;
  }

  .hero {
    padding: 4px 0 26px;
  }

  .hero-en {
    font-size: 44px;
    letter-spacing: 10px;
  }

  .hero-zh {
    margin-top: 6px;
    font-size: 13px;
    letter-spacing: 8px;
  }

  .stepper {
    margin-bottom: 24px;
  }

  .step::after {
    top: 14px;
    height: 1px;
  }

  .step-dot {
    width: 30px;
    height: 30px;
    border-width: 1px;
    font-size: 13px;
  }

  .step-label {
    margin-top: 6px;
    font-size: 12px;
  }

  .panel {
    padding: 24px;
    border-width: 1px;
    border-radius: 14px;
    margin-bottom: 20px;
  }

  .panel-title {
    margin-bottom: 16px;
    font-size: 17px;
  }

  .system-grid {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 16px;
  }

  .system-card {
    padding: 16px;
    border-width: 1px;
    border-radius: 12px;
    cursor: pointer;
    transition: transform 0.15s ease, box-shadow 0.15s ease;
  }

  .system-card:hover {
    transform: translateY(-2px);
    box-shadow: 0 10px 26px rgba(91, 70, 54, 0.1);
  }

  .system-icon-wrap {
    width: 72px;
    height: 72px;
    border-radius: 10px;
  }

  .system-icon {
    width: 56px;
    height: 56px;
  }

  .system-name {
    font-size: 16px;
  }

  .system-desc {
    font-size: 13px;
  }

  .system-check {
    top: 12px;
    right: 12px;
    width: 22px;
    height: 22px;
    font-size: 12px;
  }

  .system-lock {
    top: 12px;
    right: 12px;
    font-size: 11px;
  }

  .class-list {
    gap: 18px;
  }

  .feature-list {
    column-gap: 32px;
  }

  .class-card {
    border-width: 1px;
    border-radius: 14px;
    cursor: pointer;
    transition: transform 0.15s ease, box-shadow 0.15s ease;
  }

  .class-card:hover {
    transform: translateY(-2px);
    box-shadow: 0 12px 30px rgba(91, 70, 54, 0.14);
  }

  .class-name {
    font-size: 22px;
    letter-spacing: 1px;
  }

  .class-sub {
    margin-top: 6px;
    font-size: 12px;
  }

  .class-check {
    top: 14px;
    right: 14px;
    width: 26px;
    height: 26px;
    font-size: 14px;
  }

  .class-body {
    padding: 16px;
  }

  .class-desc {
    margin-top: 10px;
    font-size: 13px;
  }

  .feature {
    font-size: 12px;
  }

  .wizard-actions {
    gap: 12px;
  }
}
</style>
