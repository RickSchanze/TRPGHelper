<template>
  <view class="page wizard-page">
    <AppHeader current="create" />

    <view class="wizard-head">
      <view>
        <view class="wizard-title">车新卡</view>
        <view class="wizard-sub muted">引导式流程 · 进度自动保存在本机，车完才提交</view>
      </view>
      <text v-if="hasProgress" class="restart-link" @tap="restart">重新开始</text>
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

    <view v-if="resumed" class="resume muted">已恢复上次进度（第 {{ step }} 步），接着车就行。</view>

    <!-- 第一步：选择规则书 -->
    <view v-if="step === 1" class="panel">
      <view class="panel-title">选择规则书</view>
      <view class="panel-hint muted">规则书决定属性模板和后续的检定方式，之后可以随时补齐细节。</view>

      <view v-if="systemsLoading" class="muted">正在加载规则书…</view>
      <view v-else-if="systemsError" class="load-error" @tap="loadSystems">
        <text class="muted">规则书加载失败，点这里重试</text>
      </view>

      <view class="system-grid">
        <view
          v-for="system in systems"
          :key="system.code"
          class="system-card"
          :class="{ 'system-selected': draft.systemCode === system.code }"
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
            <view class="system-code muted">{{ system.code.toUpperCase() }}</view>
            <view class="system-desc muted">{{ system.description }}</view>
            <view class="system-attrs muted">{{ attributeSummary(system) }}</view>
          </view>

          <view v-if="draft.systemCode === system.code" class="system-check">✓</view>
        </view>
      </view>
    </view>

    <!-- 第二步：基本信息 -->
    <view v-else-if="step === 2" class="panel">
      <view class="panel-title">基本信息</view>
      <view class="panel-hint muted">先随便起个名字，之后随时能改。</view>

      <view class="field">
        <text class="field-label">姓名</text>
        <input v-model="draft.name" class="field-input" placeholder="角色名" maxlength="40" />
        <text class="field-action" @tap="suggestName">随机</text>
      </view>

      <view class="field">
        <text class="field-label">职业</text>
        <input v-model="draft.occupation" class="field-input" placeholder="职业 / 定位" maxlength="40" />
        <text class="field-action" @tap="suggestOccupation">随机</text>
      </view>

      <view class="field">
        <text class="field-label">年龄</text>
        <input v-model="draft.age" class="field-input" type="number" placeholder="可选" />
      </view>

      <view class="field field-column">
        <text class="field-label">简介</text>
        <textarea
          v-model="draft.summary"
          class="field-textarea"
          placeholder="一句话介绍这个角色（可选）"
          maxlength="200"
        />
      </view>
    </view>

    <!-- 第三步：属性与灵感 -->
    <view v-else-if="step === 3" class="panel">
      <view class="panel-head">
        <view class="panel-title">属性</view>
        <view class="method-actions">
          <text class="method-btn" :class="{ active: draft.method === 'roll' }" @tap="rollAttributes('roll')">
            掷骰
          </text>
          <text class="method-btn" :class="{ active: draft.method === 'standard' }" @tap="rollAttributes('standard')">
            标准数组
          </text>
        </view>
      </view>

      <view class="attr-grid">
        <view v-for="item in draft.attributes" :key="item.key" class="attr">
          <view class="attr-value">{{ item.value }}</view>
          <view class="attr-label muted">{{ item.label }}</view>
        </view>
      </view>
      <view v-if="draft.attributes.length === 0" class="muted">还没有属性，点「掷骰」生成一组。</view>

      <view class="inspiration">
        <view class="inspiration-head">
          <text class="inspiration-title">灵感</text>
          <text class="method-btn" @tap="loadInspiration">换一个</text>
        </view>
        <template v-if="draft.inspiration">
          <view class="inspiration-name">{{ draft.inspiration.title }}</view>
          <view class="muted">{{ draft.inspiration.text }}</view>
        </template>
        <view v-else class="muted">还没有灵感素材。</view>
      </view>
    </view>

    <!-- 第四步：确认 -->
    <view v-else class="panel">
      <view class="panel-title">确认这张卡</view>
      <view class="panel-hint muted">确认无误后才会提交到服务器；之后可以在卡库里继续编辑。</view>

      <view class="summary">
        <view class="summary-row">
          <text class="summary-label muted">规则书</text>
          <text>{{ currentSystem?.name || '未选择' }}</text>
        </view>
        <view class="summary-row">
          <text class="summary-label muted">姓名</text>
          <text>{{ draft.name || '未填写' }}</text>
        </view>
        <view class="summary-row">
          <text class="summary-label muted">职业</text>
          <text>{{ draft.occupation || '未填写' }}</text>
        </view>
        <view class="summary-row">
          <text class="summary-label muted">年龄</text>
          <text>{{ draft.age || '未填写' }}</text>
        </view>
        <view class="summary-row summary-attrs">
          <text class="summary-label muted">属性</text>
          <text>{{ attributeLine }}</text>
        </view>
        <view v-if="draft.summary" class="summary-row summary-attrs">
          <text class="summary-label muted">简介</text>
          <text>{{ draft.summary }}</text>
        </view>
        <view v-if="draft.inspiration" class="summary-row summary-attrs">
          <text class="summary-label muted">灵感</text>
          <text>{{ draft.inspiration.title }}</text>
        </view>
      </view>
    </view>

    <view class="wizard-actions">
      <button v-if="step > 1" class="btn btn-plain" @tap="prev">上一步</button>
      <view v-else class="action-spacer" />

      <button v-if="step < 4" class="btn" @tap="next">下一步</button>
      <button v-else class="btn" :disabled="saving" @tap="save">完成并保存</button>
    </view>
  </view>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { onHide, onLoad, onUnload } from '@dcloudio/uni-app'
import { request } from '../../api/http'
import { artworkFor } from '../../config/rulebooks'
import type { GameSystem, GeneratedAttributes, Inspiration } from '../../api/types'
import {
  clearDraft,
  emptyDraft,
  loadDraft,
  saveDraft,
  type AttributeMethod,
  type CreateDraft,
  type CreateStep,
} from '../../stores/createDraft'
import AppHeader from '../../components/AppHeader/AppHeader.vue'

const stepLabels = ['规则书', '基本信息', '属性', '确认']

const draft = ref<CreateDraft>(emptyDraft())
const systems = ref<GameSystem[]>([])
const systemsLoading = ref(false)
const systemsError = ref(false)
const resumed = ref(false)
const ready = ref(false)
const saving = ref(false)
const failedIcons = ref<Record<string, boolean>>({})

const step = computed(() => draft.value.step)
const currentSystem = computed(
  () => systems.value.find((system) => system.code === draft.value.systemCode) ?? null,
)
const hasProgress = computed(
  () =>
    draft.value.step > 1 ||
    Boolean(draft.value.systemCode) ||
    Boolean(draft.value.name.trim()) ||
    draft.value.attributes.length > 0,
)
const attributeLine = computed(
  () => draft.value.attributes.map((item) => `${item.label} ${item.value}`).join(' · ') || '未生成',
)

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
    resumed.value = restored.step > 1 || Boolean(restored.systemCode)
  }

  await loadSystems()

  // 规则书被移除或改名时，把草稿回退到第一步
  if (draft.value.systemCode && !systems.value.some((system) => system.code === draft.value.systemCode)) {
    draft.value.systemCode = null
    draft.value.step = 1
    draft.value.attributes = []
    draft.value.inspiration = null
  }

  if (draft.value.systemCode) {
    if (draft.value.attributes.length === 0) {
      await rollAttributes(draft.value.method)
    }
    if (!draft.value.inspiration) {
      void loadInspiration()
    }
  }

  ready.value = true
}

async function loadSystems(): Promise<void> {
  systemsLoading.value = true
  systemsError.value = false

  try {
    systems.value = await request<GameSystem[]>({ url: '/api/systems', auth: false })
  } catch {
    systemsError.value = true
  } finally {
    systemsLoading.value = false
  }
}

function attributeSummary(system: GameSystem): string {
  if (system.attributes.length === 0) {
    return ''
  }

  const labels = system.attributes.slice(0, 6).map((item) => item.label).join(' / ')
  return system.attributes.length > 6 ? `${labels} …` : labels
}

function markIconFailed(code: string): void {
  failedIcons.value[code] = true
}

async function pickSystem(system: GameSystem): Promise<void> {
  if (draft.value.systemCode === system.code) {
    return
  }

  draft.value.systemCode = system.code
  draft.value.attributes = []
  draft.value.inspiration = null
  draft.value.method = 'roll'

  await rollAttributes('roll')
  void loadInspiration()
}

async function rollAttributes(method: AttributeMethod): Promise<void> {
  const system = currentSystem.value
  if (!system) {
    return
  }

  try {
    const result = await request<GeneratedAttributes>({
      url: '/api/generator/attributes',
      method: 'POST',
      auth: false,
      data: { systemCode: system.code, method },
    })
    draft.value.attributes = result.attributes
    draft.value.method = method
  } catch {
    // 错误提示已统一处理
  }
}

async function suggestName(): Promise<void> {
  const system = currentSystem.value
  if (!system) {
    return
  }

  try {
    const names = await request<string[]>({
      url: `/api/generator/names?systemCode=${system.code}&count=1`,
      auth: false,
    })
    if (names.length > 0) {
      draft.value.name = names[0]
    }
  } catch {
    // ignore
  }
}

async function suggestOccupation(): Promise<void> {
  const system = currentSystem.value
  if (!system) {
    return
  }

  try {
    const items = await request<string[]>({
      url: `/api/generator/occupations?systemCode=${system.code}&count=1`,
      auth: false,
    })
    if (items.length > 0) {
      draft.value.occupation = items[0]
    }
  } catch {
    // ignore
  }
}

async function loadInspiration(): Promise<void> {
  const system = currentSystem.value
  if (!system) {
    return
  }

  try {
    const items = await request<Inspiration[]>({
      url: `/api/generator/inspirations?systemCode=${system.code}&count=1`,
      auth: false,
    })
    draft.value.inspiration = items[0] ?? null
  } catch {
    // ignore
  }
}

function next(): void {
  if (step.value === 1 && !draft.value.systemCode) {
    toast('先选一本规则书')
    return
  }

  if (step.value === 2 && !draft.value.name.trim()) {
    toast('先给角色起个名字')
    return
  }

  if (step.value === 3 && draft.value.attributes.length === 0) {
    toast('先掷一组属性')
    return
  }

  if (step.value >= 4) {
    return
  }

  draft.value.step = (step.value + 1) as CreateStep
  saveDraft(draft.value)

  if (draft.value.step === 3 && !draft.value.inspiration) {
    void loadInspiration()
  }

  scrollTop()
}

function prev(): void {
  if (step.value <= 1) {
    return
  }

  draft.value.step = (step.value - 1) as CreateStep
  scrollTop()
}

function restart(): void {
  uni.showModal({
    title: '清空重来',
    content: '本机保存的草稿会被清空，确定吗？',
    success: (result) => {
      if (!result.confirm) {
        return
      }

      clearDraft()
      draft.value = emptyDraft()
      resumed.value = false
      failedIcons.value = {}
      scrollTop()
    },
  })
}

async function save(): Promise<void> {
  if (!draft.value.systemCode) {
    draft.value.step = 1
    toast('规则书丢失，请重新选择')
    return
  }

  if (!draft.value.name.trim()) {
    draft.value.step = 2
    toast('先给角色起个名字')
    return
  }

  saving.value = true
  try {
    const age = Number.parseInt(draft.value.age, 10)
    const created = await request<{ id: string }>({
      url: '/api/characters',
      method: 'POST',
      data: {
        systemCode: draft.value.systemCode,
        name: draft.value.name.trim(),
        occupation: draft.value.occupation.trim() || null,
        age: Number.isNaN(age) ? null : age,
        summary: draft.value.summary.trim() || null,
        sheet: { attributes: draft.value.attributes },
      },
    })

    clearDraft()
    uni.redirectTo({ url: `/pages/detail/detail?id=${created.id}` })
  } catch {
    // 提交失败时保留本地草稿，用户可以继续改或重试
  } finally {
    saving.value = false
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
.wizard-head {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  margin-bottom: 24rpx;
}

.wizard-title {
  font-size: 44rpx;
  font-weight: 600;
}

.wizard-sub {
  margin-top: 6rpx;
  font-size: 22rpx;
}

.restart-link {
  padding-top: 8rpx;
  font-size: 24rpx;
  color: $trpg-accent;
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

.resume {
  margin-bottom: 20rpx;
  padding: 16rpx 20rpx;
  border-radius: 12rpx;
  background: #f7ecd9;
  font-size: 22rpx;
}

.panel {
  padding: 28rpx;
  margin-bottom: 24rpx;
  border: 2rpx solid $trpg-line;
  border-radius: 20rpx;
  background: #fffdf8;
}

.panel-title {
  font-size: 30rpx;
  font-weight: 600;
}

.panel-hint {
  margin: 8rpx 0 20rpx;
  font-size: 22rpx;
}

.panel-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
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

.system-code {
  font-size: 20rpx;
  letter-spacing: 2rpx;
}

.system-desc {
  margin-top: 6rpx;
  font-size: 22rpx;
}

.system-attrs {
  margin-top: 8rpx;
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
  padding: 24rpx 0;
}

.field {
  display: flex;
  align-items: center;
  min-height: 88rpx;
  border-bottom: 2rpx solid $trpg-line;
}

.field:last-child {
  border-bottom: none;
}

.field-column {
  flex-direction: column;
  align-items: flex-start;
  padding: 16rpx 0;
}

.field-label {
  width: 120rpx;
  font-size: 26rpx;
  color: $trpg-muted;
}

.field-input {
  flex: 1;
  height: 88rpx;
}

.field-textarea {
  width: 100%;
  height: 140rpx;
  margin-top: 8rpx;
  font-size: 26rpx;
}

.field-action {
  font-size: 24rpx;
  color: $trpg-accent;
}

.method-actions {
  display: flex;
  gap: 24rpx;
}

.method-btn {
  font-size: 24rpx;
  color: $trpg-accent;
}

.method-btn.active {
  font-weight: 600;
  text-decoration: underline;
}

.attr-grid {
  display: flex;
  flex-wrap: wrap;
  margin: 8rpx 0 20rpx;
}

.attr {
  width: 33.33%;
  padding: 12rpx 0;
  text-align: center;
}

.attr-value {
  font-size: 44rpx;
  font-weight: 600;
  color: $trpg-accent-deep;
}

.attr-label {
  font-size: 22rpx;
}

.inspiration {
  margin-top: 12rpx;
  padding: 20rpx;
  border-radius: 16rpx;
  background: #f9f4e8;
}

.inspiration-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 8rpx;
}

.inspiration-title {
  font-size: 26rpx;
  font-weight: 600;
}

.inspiration-name {
  font-size: 26rpx;
  font-weight: 600;
}

.summary {
  display: flex;
  flex-direction: column;
  gap: 14rpx;
  margin-top: 12rpx;
}

.summary-row {
  display: flex;
  gap: 16rpx;
  font-size: 26rpx;
}

.summary-label {
  width: 120rpx;
  flex-shrink: 0;
}

.summary-attrs {
  align-items: flex-start;
}

.summary-attrs text:last-child {
  flex: 1;
}

.wizard-actions {
  display: flex;
  gap: 20rpx;
}

.wizard-actions .btn {
  flex: 1;
}

.action-spacer {
  flex: 1;
}

@media (min-width: 900px) {
  .page {
    max-width: 860px;
  }

  .wizard-title {
    font-size: 30px;
  }

  .wizard-sub {
    font-size: 13px;
  }

  .restart-link {
    font-size: 13px;
    cursor: pointer;
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

  .resume {
    padding: 10px 14px;
    border-radius: 8px;
    font-size: 13px;
  }

  .panel {
    padding: 24px;
    border-width: 1px;
    border-radius: 14px;
    margin-bottom: 20px;
  }

  .panel-title {
    font-size: 17px;
  }

  .panel-hint {
    margin: 6px 0 16px;
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
    box-shadow: 0 10px 26px rgba(91, 70, 54, 0.12);
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

  .system-code {
    font-size: 11px;
  }

  .system-desc {
    font-size: 13px;
  }

  .system-attrs {
    font-size: 11px;
  }

  .system-check {
    top: 12px;
    right: 12px;
    width: 22px;
    height: 22px;
    font-size: 12px;
  }

  .field {
    min-height: 52px;
    border-bottom-width: 1px;
  }

  .field-label {
    width: 72px;
    font-size: 14px;
  }

  .field-input {
    height: 52px;
    font-size: 14px;
  }

  .field-textarea {
    height: 96px;
    font-size: 14px;
  }

  .field-action {
    font-size: 13px;
    cursor: pointer;
  }

  .method-btn {
    font-size: 13px;
    cursor: pointer;
  }

  .attr {
    width: 20%;
    padding: 10px 0;
  }

  .attr-value {
    font-size: 28px;
  }

  .attr-label {
    font-size: 12px;
  }

  .inspiration {
    padding: 16px;
    border-radius: 10px;
  }

  .inspiration-title,
  .inspiration-name {
    font-size: 14px;
  }

  .summary-row {
    font-size: 14px;
  }

  .summary-label {
    width: 72px;
  }

  .wizard-actions {
    gap: 12px;
  }
}
</style>
