<template>
  <view class="page wizard-page">
    <AppHeader current="create" />

    <view class="hero">
      <view class="hero-en">WHO ARE YOU</view>
      <view class="hero-zh">你是谁</view>
    </view>

    <!-- 前置条件：选择规则书。它不算车卡流程里的步骤。 -->
    <view v-if="!started" class="panel">
      <view class="panel-title">选择规则书</view>
      <view class="muted rulebook-hint">规则书决定属性模板和素材范围，选完才进入车卡流程，它本身不算一步。</view>

      <view class="system-grid">
        <view
          v-for="system in systems"
          :key="system.code"
          class="system-card"
          :class="{
            'system-selected': draft.systemCode === system.code,
            'system-locked': !system.available,
          }"
          @tap="pickSystem(system)"
        >
          <view class="system-icon-wrap" :style="{ backgroundColor: system.accent || DEFAULT_ACCENT }">
            <image
              v-if="system.iconUrl && !failedIcons[system.code]"
              class="system-icon"
              :src="system.iconUrl"
              mode="aspectFit"
              @error="markIconFailed(system.code)"
            />
            <text v-else class="system-icon-fallback">{{ system.name.slice(0, 1) }}</text>
          </view>

          <view class="system-info">
            <view class="system-name">
              <KeywordText :text="system.name" />
            </view>
            <view class="system-desc muted">
              <KeywordText :text="system.description" />
            </view>
          </view>

          <text v-if="!system.available" class="system-lock muted">即将开放</text>
          <view v-else-if="draft.systemCode === system.code" class="system-check">✓</view>
        </view>
      </view>

      <view class="rulebook-actions">
        <button class="btn" :disabled="!draft.systemCode || flowLoading" @tap="startFlow">
          {{ flowLoading ? '正在加载流程…' : '开始车卡' }}
        </button>
      </view>
    </view>

    <!-- 车卡流程：步骤数量和顺序来自管理台配置 -->
    <template v-else>
      <view v-if="flowLoading" class="panel">
        <text class="muted">正在加载车卡流程…</text>
      </view>

      <view v-else-if="flowError" class="panel load-error" @tap="loadFlow">
        <text class="muted">流程加载失败，点这里重试</text>
      </view>

      <view v-else-if="!steps.length" class="panel">
        <view class="panel-title">这个规则书还没配置车卡流程</view>
        <text class="muted">去管理台的「车卡流程」里加一步（比如「选择职业」），再回来刷新。</text>
      </view>

      <template v-else>
        <view class="stepper">
          <view
            v-for="(item, index) in steps"
            :key="item.id"
            class="step"
            :class="{ 'step-active': step === index + 1, 'step-done': step > index + 1 }"
          >
            <view class="step-dot">{{ step > index + 1 ? '✓' : index + 1 }}</view>
            <KeywordText class="step-label" :text="item.title" />
          </view>
        </view>

        <!-- 职业步骤：唯一已实现的面板 -->
        <view v-if="isClassStep" class="panel">
          <view class="panel-title">
            <KeywordText :text="currentStep?.title || '选择职业'" />
          </view>
          <view v-if="currentStep?.description" class="muted step-hint">
            <KeywordText :text="currentStep.description" />
          </view>

          <view v-if="classesLoading" class="muted">正在加载职业…</view>
          <view v-else-if="classesError" class="load-error" @tap="loadClasses">
            <text class="muted">职业加载失败，点这里重试</text>
          </view>

          <view v-else class="class-list">
            <view v-for="item in classes" :key="item.code" class="class-card" @tap="pickClass(item)">
              <view class="class-cover-wrap">
                <image
                  v-if="coverFor(item) && !failedCovers[item.code]"
                  class="class-cover"
                  :src="coverFor(item)"
                  mode="aspectFit"
                  @error="failedCovers[item.code] = true"
                />
                <view
                  v-else
                  class="class-cover class-cover-fallback"
                  :style="{ backgroundColor: item.accent || DEFAULT_ACCENT }"
                >
                  <text>{{ item.name.slice(0, 1) }}</text>
                </view>
              </view>

              <view class="class-body">
                <view class="class-head">
                  <view class="class-name">
                    <KeywordText :text="item.name" />
                  </view>
                  <view class="class-detail" @tap.stop="openDetail(item)">详情 ›</view>
                </view>
                <view class="class-sub">
                  <text>生命骰 </text>
                  <KeywordText :text="item.hitDie" />
                  <text> · 主要属性 </text>
                  <KeywordText :text="item.primaryAbility" />
                </view>
                <view class="class-desc">
                  <KeywordText :text="item.description" />
                </view>
                <view class="feature-list">
                  <view v-for="feature in item.features" :key="feature" class="feature">
                    <text class="feature-dot">◆</text>
                    <KeywordText class="feature-text" :text="feature" />
                  </view>
                </view>
              </view>

              <view v-if="draft.classCode === item.code" class="class-ribbon">已选择</view>
            </view>
          </view>
        </view>

        <!-- 其它类型的步骤：面板还没做，先按配置占位 -->
        <view v-else class="panel placeholder-panel">
          <view class="panel-title">
            <KeywordText :text="currentStep?.title" />
          </view>
          <KeywordText class="muted" :text="currentStep?.description || '这一步的面板还在开发中，先跟着流程往下走。'" />
          <text class="placeholder-kind muted">步骤类型：{{ currentStep?.kind }}</text>
        </view>
      </template>
    </template>

    <view v-if="isClassStep && classes.length > 1" class="class-rail">
      <view
        v-for="(item, index) in classes"
        :key="item.code"
        class="rail-item"
        :class="{ active: activeClassIndex === index }"
        @tap="scrollToClass(index)"
      >
        <KeywordText class="rail-name" :text="item.name" />
        <text v-if="draft.classCode === item.code" class="rail-check">✓</text>
      </view>
    </view>

    <view v-if="started && steps.length" class="wizard-actions">
      <button class="btn btn-plain" :disabled="finishing" @tap="prev">上一步</button>
      <button class="btn" :disabled="!canNext || finishing" @tap="next">
        {{ isLastStep ? (finishing ? '提交中…' : '完成') : '下一步' }}
      </button>
    </view>

    <KeywordModal />

    <ClassDetailModal
      :visible="detailVisible"
      :option="detailClass"
      :system-code="draft.systemCode"
      @close="detailVisible = false"
    />
  </view>
</template>

<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { onHide, onLoad, onPageScroll, onUnload } from '@dcloudio/uni-app'
import { request } from '../../api/http'
import type { ClassOption, CreationFlow, CreationStep, GameSystem } from '../../api/types'
import { clearDraft, emptyDraft, loadDraft, saveDraft, type CreateDraft } from '../../stores/createDraft'
import AppHeader from '../../components/AppHeader/AppHeader.vue'
import KeywordText from '../../components/KeywordText/KeywordText.vue'
import KeywordModal from '../../components/KeywordModal/KeywordModal.vue'
import ClassDetailModal from '../../components/ClassDetailModal/ClassDetailModal.vue'

/** 内容里没配主色时的兜底（纯展示默认值，不是内容） */
const DEFAULT_ACCENT = '#5b4636'

const draft = ref<CreateDraft>(emptyDraft())
const systems = ref<GameSystem[]>([])
const steps = ref<CreationStep[]>([])
const classes = ref<ClassOption[]>([])
const flowLoading = ref(false)
const flowError = ref(false)
const classesLoading = ref(false)
const classesError = ref(false)
const ready = ref(false)
const started = ref(false)
const finishing = ref(false)
const failedIcons = ref<Record<string, boolean>>({})
const failedCovers = ref<Record<string, boolean>>({})
const activeClassIndex = ref(0)
const cardTops = ref<number[]>([])
const jumping = ref(false)
const detailVisible = ref(false)
const detailClass = ref<ClassOption | null>(null)

let jumpTimer: ReturnType<typeof setTimeout> | null = null
let scrollTimer: ReturnType<typeof setTimeout> | null = null

const step = computed(() => draft.value.step)
const currentStep = computed<CreationStep | null>(() => steps.value[draft.value.step - 1] ?? null)
const isClassStep = computed(() => currentStep.value?.kind === 'class')
const isLastStep = computed(() => steps.value.length > 0 && draft.value.step >= steps.value.length)
const canNext = computed(() => {
  if (!currentStep.value) {
    return false
  }

  return isClassStep.value ? Boolean(draft.value.classCode) : true
})

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

onPageScroll((event) => {
  // 跳转动画期间不跟着滚动事件改高亮，避免来回闪
  if (jumping.value || cardTops.value.length === 0) {
    return
  }

  if (scrollTimer) {
    clearTimeout(scrollTimer)
  }

  scrollTimer = setTimeout(() => {
    const threshold = event.scrollTop + 160
    let index = 0
    cardTops.value.forEach((top, position) => {
      if (top <= threshold) {
        index = position
      }
    })
    activeClassIndex.value = index
  }, 60)
})

function measureCards(): void {
  const query = uni.createSelectorQuery()
  query.selectAll('.class-card').boundingClientRect()
  query.selectViewport().scrollOffset()
  query.exec((result) => {
    const rects = (result?.[0] ?? []) as Array<{ top: number }>
    const scrollTop = (result?.[1]?.scrollTop ?? 0) as number
    cardTops.value = rects.map((rect) => rect.top + scrollTop)
  })
}

function scrollToClass(index: number): void {
  const top = cardTops.value[index]
  if (top === undefined) {
    return
  }

  jumping.value = true
  activeClassIndex.value = index

  if (jumpTimer) {
    clearTimeout(jumpTimer)
  }

  jumpTimer = setTimeout(() => {
    jumping.value = false
  }, 420)

  const isDesktop = uni.getSystemInfoSync().windowWidth >= 900
  uni.pageScrollTo({
    scrollTop: Math.max(0, top - (isDesktop ? 96 : 24)),
    duration: 300,
  })
}

async function init(): Promise<void> {
  const restored = loadDraft()
  if (restored) {
    draft.value = restored
  }

  await loadSystems()

  if (draft.value.systemCode && draft.value.systemCode !== 'dnd5e') {
    draft.value.systemCode = null
    draft.value.classCode = null
    draft.value.className = ''
  }

  if (draft.value.systemCode) {
    await loadFlow()
    await loadClasses()
    started.value = true

    if (steps.value.length === 0) {
      draft.value.step = 1
    } else {
      draft.value.step = Math.min(Math.max(1, draft.value.step), steps.value.length)
    }
  } else {
    draft.value.step = 1
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

async function loadFlow(): Promise<void> {
  if (!draft.value.systemCode) {
    steps.value = []
    return
  }

  flowLoading.value = true
  flowError.value = false

  try {
    const flow = await request<CreationFlow>({
      url: `/api/creation-flow?systemCode=${draft.value.systemCode}`,
      auth: false,
    })
    steps.value = flow.steps ?? []
  } catch {
    flowError.value = true
    steps.value = []
  } finally {
    flowLoading.value = false
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

    await nextTick()
    setTimeout(measureCards, 250)
  } catch {
    classesError.value = true
  } finally {
    classesLoading.value = false
  }
}

/** 职业配图完全来自内容配置（generator.xlsx 的 imageUrl），没配就显示主色块。 */
function coverFor(option: ClassOption): string {
  return (option.imageUrl ?? '').trim()
}

function markIconFailed(code: string): void {
  failedIcons.value[code] = true
}

function pickSystem(system: GameSystem): void {
  if (!system.available) {
    return
  }

  if (draft.value.systemCode !== system.code) {
    draft.value.systemCode = system.code
    draft.value.classCode = null
    draft.value.className = ''
    classes.value = []
    failedCovers.value = {}
    void loadFlow()
    void loadClasses()
  }
}

async function startFlow(): Promise<void> {
  if (!draft.value.systemCode) {
    toast('先选规则书')
    return
  }

  if (flowLoading.value) {
    return
  }

  await loadFlow()
  if (steps.value.length === 0) {
    toast('这个规则书还没配置车卡流程')
    return
  }

  if (!classes.value.length) {
    void loadClasses()
  }

  started.value = true
  draft.value.step = 1
  saveDraft(draft.value)
  scrollTop()
}

function pickClass(option: ClassOption): void {
  draft.value.classCode = option.code
  draft.value.className = option.name
}

function openDetail(option: ClassOption): void {
  detailClass.value = option
  detailVisible.value = true
}

function next(): void {
  if (!canNext.value) {
    toast(isClassStep.value ? '先选一个职业' : '这一步还不能继续')
    return
  }

  if (isLastStep.value) {
    void finish()
    return
  }

  draft.value.step = Math.min(step.value + 1, steps.value.length)
  saveDraft(draft.value)
  scrollTop()
}

function prev(): void {
  if (step.value > 1) {
    draft.value.step = step.value - 1
    saveDraft(draft.value)
    scrollTop()
    return
  }

  // 第一步再往前 = 回到选规则书（不算步骤），职业选择先留着
  started.value = false
  draft.value.step = 1
  saveDraft(draft.value)
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

.rulebook-hint {
  margin-bottom: 20rpx;
  font-size: 22rpx;
  line-height: 1.5;
}

.rulebook-actions {
  margin-top: 24rpx;
}

.step-hint {
  margin: -8rpx 0 20rpx;
  font-size: 22rpx;
}

.placeholder-panel {
  display: flex;
  flex-direction: column;
  gap: 12rpx;
}

.placeholder-kind {
  font-size: 20rpx;
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

.class-rail {
  position: fixed;
  top: 50%;
  right: 12rpx;
  z-index: 60;
  display: flex;
  flex-direction: column;
  gap: 8rpx;
  padding: 10rpx;
  border: 2rpx solid $trpg-line;
  border-radius: 999rpx;
  background: rgba(255, 253, 248, 0.94);
  box-shadow: 0 10rpx 30rpx rgba(91, 70, 54, 0.14);
  transform: translateY(-50%);
}

.rail-item {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 2rpx;
  min-width: 52rpx;
  min-height: 52rpx;
  padding: 0 8rpx;
  border-radius: 10rpx;
  color: $trpg-muted;
}

.rail-item.active {
  color: $trpg-ink;
}

.rail-name {
  font-size: 18rpx;
  transition: font-size 0.15s ease;
}

.rail-item.active .rail-name {
  font-size: 26rpx;
  font-weight: 600;
}

.rail-check {
  font-size: 14rpx;
  color: $trpg-accent;
}

.class-card {
  position: relative;
  display: flex;
  flex-direction: column;
  height: 100%;
  overflow: hidden;
  border: 2rpx solid $trpg-line;
  border-radius: 20rpx;
  background: #fff;
}

.class-ribbon {
  position: absolute;
  right: -72rpx;
  bottom: 34rpx;
  z-index: 3;
  width: 230rpx;
  padding: 8rpx 0;
  background: $trpg-accent;
  color: #fffaf0;
  font-size: 20rpx;
  letter-spacing: 4rpx;
  text-align: center;
  transform: rotate(-45deg);
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

.class-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12rpx;
}

.class-detail {
  flex-shrink: 0;
  padding: 4rpx 16rpx;
  border: 2rpx solid #d8c6a5;
  border-radius: 999rpx;
  font-size: 20rpx;
  color: $trpg-accent-deep;
}

.class-detail:active {
  opacity: 0.7;
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

.class-body {
  flex: 1;
  padding: 20rpx 20rpx 56rpx;
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

  .rulebook-hint,
  .step-hint {
    font-size: 13px;
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

  .class-rail {
    right: max(16px, calc((100vw - 860px) / 2 - 68px));
    gap: 6px;
    padding: 8px;
    border-width: 1px;
  }

  .rail-item {
    min-width: 36px;
    min-height: 36px;
    padding: 0 6px;
    cursor: pointer;
  }

  .rail-item:hover {
    color: $trpg-accent;
  }

  .rail-name {
    font-size: 12px;
  }

  .rail-item.active .rail-name {
    font-size: 16px;
  }

  .rail-check {
    font-size: 10px;
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

  .class-detail {
    padding: 3px 12px;
    border-width: 1px;
    font-size: 12px;
    cursor: pointer;
  }

  .class-sub {
    margin-top: 6px;
    font-size: 12px;
  }

  .class-body {
    padding: 16px 16px 44px;
  }

  .class-ribbon {
    right: -56px;
    bottom: 32px;
    width: 170px;
    padding: 6px 0;
    font-size: 12px;
    letter-spacing: 3px;
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
