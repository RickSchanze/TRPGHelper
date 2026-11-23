<template>
  <view class="page">
    <AppHeader current="create" />

    <view class="section">
      <view class="section-title">规则系统</view>
      <picker :range="systems" range-key="name" :value="systemIndex" @change="onSystemChange">
        <view class="picker">
          <text>{{ currentSystem?.name || '选择规则系统' }}</text>
          <text class="muted">▾</text>
        </view>
      </picker>
      <view v-if="currentSystem?.description" class="muted tip">{{ currentSystem.description }}</view>
    </view>

    <view class="section">
      <view class="section-title">基本信息</view>

      <view class="field">
        <text class="field-label">姓名</text>
        <input v-model="name" class="field-input" placeholder="角色名" maxlength="40" />
        <text class="field-action" @tap="suggestName">随机</text>
      </view>

      <view class="field">
        <text class="field-label">职业</text>
        <input v-model="occupation" class="field-input" placeholder="职业 / 角色定位" maxlength="40" />
        <text class="field-action" @tap="suggestOccupation">随机</text>
      </view>

      <view class="field">
        <text class="field-label">年龄</text>
        <input v-model="ageText" class="field-input" type="number" placeholder="可选" />
      </view>
    </view>

    <view class="section">
      <view class="section-head">
        <view class="section-title">属性</view>
        <view class="roll-actions">
          <text class="roll-btn" @tap="rollAttributes('roll')">掷属性</text>
          <text class="roll-btn" @tap="rollAttributes('standard')">标准数组</text>
        </view>
      </view>

      <view class="attr-grid">
        <view v-for="attribute in attributes" :key="attribute.key" class="attr">
          <view class="attr-value">{{ attribute.value }}</view>
          <view class="attr-label muted">{{ attribute.label }}</view>
        </view>
      </view>

      <view v-if="attributes.length === 0" class="muted tip">还没有属性，点「掷属性」生成一组。</view>
    </view>

    <view class="section">
      <view class="section-head">
        <view class="section-title">随便找点灵感</view>
        <text class="roll-btn" @tap="loadInspiration">换一个</text>
      </view>
      <view v-if="inspiration" class="inspiration">
        <view class="inspiration-title">{{ inspiration.title }}</view>
        <view class="muted">{{ inspiration.text }}</view>
      </view>
      <view v-else class="muted tip">暂无灵感素材。</view>
    </view>

    <button class="btn save" :disabled="saving" @tap="save">保存这张卡</button>
  </view>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { request } from '../../api/http'
import type { GameSystem, GeneratedAttributes, Inspiration, SheetAttribute } from '../../api/types'
import AppHeader from '../../components/AppHeader/AppHeader.vue'

const systems = ref<GameSystem[]>([])
const systemIndex = ref(0)
const currentSystem = computed<GameSystem | undefined>(() => systems.value[systemIndex.value])

const name = ref('')
const occupation = ref('')
const ageText = ref('')
const attributes = ref<SheetAttribute[]>([])
const inspiration = ref<Inspiration | null>(null)
const saving = ref(false)

async function loadSystems(): Promise<void> {
  try {
    systems.value = await request<GameSystem[]>({ url: '/api/systems', auth: false })
  } catch {
    return
  }

  if (systems.value.length > 0) {
    await rollAttributes('roll')
    void loadInspiration()
  }
}

function onSystemChange(event: { detail: { value: string | number } }): void {
  systemIndex.value = Number(event.detail.value)
  attributes.value = []
  void rollAttributes('roll')
  void suggestName()
  void loadInspiration()
}

async function rollAttributes(method: 'roll' | 'standard'): Promise<void> {
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
    attributes.value = result.attributes
  } catch {
    // ignore
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
      name.value = names[0]
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
      occupation.value = items[0]
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
    inspiration.value = items[0] ?? null
  } catch {
    // ignore
  }
}

async function save(): Promise<void> {
  const system = currentSystem.value
  if (!system) {
    uni.showToast({ title: '先选择规则系统', icon: 'none' })
    return
  }

  if (!name.value.trim()) {
    uni.showToast({ title: '先给角色起个名字', icon: 'none' })
    return
  }

  saving.value = true
  try {
    const age = Number.parseInt(ageText.value, 10)
    const created = await request<{ id: string }>({
      url: '/api/characters',
      method: 'POST',
      data: {
        systemCode: system.code,
        name: name.value.trim(),
        occupation: occupation.value.trim() || null,
        age: Number.isNaN(age) ? null : age,
        sheet: { attributes: attributes.value },
      },
    })

    uni.redirectTo({ url: `/pages/detail/detail?id=${created.id}` })
  } catch {
    // 错误提示已统一处理
  } finally {
    saving.value = false
  }
}

onLoad(() => {
  void loadSystems()
})
</script>

<style lang="scss" scoped>
.section {
  background: #fffdf8;
  border: 2rpx solid $trpg-line;
  border-radius: 20rpx;
  padding: 28rpx;
  margin-bottom: 24rpx;
}

.section-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.section-title {
  font-size: 30rpx;
  font-weight: 600;
  margin-bottom: 16rpx;
}

.section-head .section-title {
  margin-bottom: 0;
}

.picker {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 80rpx;
  padding: 0 24rpx;
  border-radius: 16rpx;
  border: 2rpx solid $trpg-line;
  background: #fff;
}

.tip {
  margin-top: 16rpx;
  font-size: 22rpx;
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

.field-label {
  width: 120rpx;
  font-size: 26rpx;
  color: $trpg-muted;
}

.field-input {
  flex: 1;
  height: 88rpx;
}

.field-action {
  font-size: 24rpx;
  color: $trpg-accent;
  padding-left: 16rpx;
}

.roll-actions {
  display: flex;
  gap: 24rpx;
}

.roll-btn {
  font-size: 24rpx;
  color: $trpg-accent;
}

.attr-grid {
  display: flex;
  flex-wrap: wrap;
  margin-top: 20rpx;
}

.attr {
  width: 33.33%;
  text-align: center;
  padding: 16rpx 0;
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
  margin-top: 20rpx;
  padding: 24rpx;
  background: #f9f4e8;
  border-radius: 16rpx;
}

.inspiration-title {
  font-size: 28rpx;
  font-weight: 600;
  margin-bottom: 8rpx;
}

.save {
  margin-top: 12rpx;
}

@media (min-width: 900px) {
  .page {
    max-width: 860px;
  }

  .section {
    border-width: 1px;
    border-radius: 14px;
    padding: 24px;
    margin-bottom: 20px;
  }

  .section-title {
    font-size: 17px;
    margin-bottom: 14px;
  }

  .section-head .section-title {
    margin-bottom: 0;
  }

  .picker {
    height: 48px;
    padding: 0 16px;
    border-radius: 10px;
    border-width: 1px;
    font-size: 14px;
    cursor: pointer;
  }

  .tip {
    font-size: 13px;
    margin-top: 10px;
  }

  .field {
    min-height: 56px;
  }

  .field-label {
    width: 72px;
    font-size: 14px;
  }

  .field-input {
    height: 56px;
    font-size: 14px;
  }

  .field-action {
    font-size: 13px;
    cursor: pointer;
  }

  .roll-btn {
    font-size: 13px;
    cursor: pointer;
  }

  .attr {
    padding: 14px 0;
  }

  .attr-value {
    font-size: 30px;
  }

  .attr-label {
    font-size: 12px;
  }

  .inspiration {
    padding: 18px;
    border-radius: 10px;
  }

  .inspiration-title {
    font-size: 15px;
  }

  .save {
    margin-top: 8px;
  }
}
</style>
