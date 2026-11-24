<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { UploadRequestOptions } from 'element-plus'
import { errorMessage, http, resolveAssetUrl } from '../api/client'
import type {
  AdminGeneratorEntry,
  AdminSystem,
  GeneratorKindKey,
  PagedResult,
  SaveGeneratorEntryPayload,
} from '../api/types'

interface KindMeta {
  key: GeneratorKindKey
  label: string
  hint: string
  categoryHint: string
  defaultCategory: string
}

const kinds: KindMeta[] = [
  {
    key: 'namePart',
    label: '姓名片段',
    hint: '按分类拼名字：surname / given / first / epithet，和规则系统的姓名模板配合。',
    categoryHint: 'surname',
    defaultCategory: 'surname',
  },
  {
    key: 'occupation',
    label: '职业',
    hint: '随机职业池，和「背景」一起被职业生成器抽取。',
    categoryHint: 'occupation',
    defaultCategory: 'occupation',
  },
  {
    key: 'background',
    label: '背景',
    hint: '随机职业池的一部分，例如士兵、贵族。',
    categoryHint: 'background',
    defaultCategory: 'background',
  },
  {
    key: 'class',
    label: '职业卡',
    hint: '分类存 code（如 ranger），扩展字段存描述、生命骰、主属性、特性列表。',
    categoryHint: 'ranger',
    defaultCategory: 'ranger',
  },
  {
    key: 'race',
    label: '种族',
    hint: '分类存 code（如 elf），扩展字段存描述、属性加值、速度。',
    categoryHint: 'elf',
    defaultCategory: 'elf',
  },
  {
    key: 'inspiration',
    label: '灵感',
    hint: '内容存标题，扩展字段 text 存正文（故事钩子）。',
    categoryHint: 'hook',
    defaultCategory: 'hook',
  },
]

interface EntryForm {
  systemCode: string
  category: string
  value: string
  sortOrder: number
  isEnabled: boolean
  description: string
  hitDie: string
  primaryAbility: string
  featuresText: string
  imageUrl: string
  abilityBonus: string
  speed: number
  text: string
}

const systems = ref<AdminSystem[]>([])
const entries = ref<AdminGeneratorEntry[]>([])
const total = ref(0)
const page = ref(1)
const pageSize = ref(20)
const loading = ref(false)
const dialogVisible = ref(false)
const saving = ref(false)
const editingId = ref<string | null>(null)
const activeKind = ref<GeneratorKindKey>('namePart')

const filters = reactive({
  systemCode: '',
  keyword: '',
  enabled: '' as '' | 'true' | 'false',
})

const form = reactive<EntryForm>(emptyForm())

const currentKind = computed(() => kinds.find((kind) => kind.key === activeKind.value) ?? kinds[0])

const hasExtraFields = computed(() => ['class', 'race', 'inspiration'].includes(activeKind.value))

function emptyForm(): EntryForm {
  return {
    systemCode: '',
    category: 'surname',
    value: '',
    sortOrder: 100,
    isEnabled: true,
    description: '',
    hitDie: 'd10',
    primaryAbility: '',
    featuresText: '',
    imageUrl: '',
    abilityBonus: '',
    speed: 30,
    text: '',
  }
}

function fillForm(entry: AdminGeneratorEntry): void {
  const extra = (entry.extra ?? {}) as Record<string, unknown>
  const features = Array.isArray(extra.features) ? (extra.features as string[]) : []

  Object.assign(form, {
    systemCode: entry.systemCode ?? '',
    category: entry.category,
    value: entry.value,
    sortOrder: entry.sortOrder,
    isEnabled: entry.isEnabled,
    description: typeof extra.description === 'string' ? extra.description : '',
    hitDie: typeof extra.hitDie === 'string' ? extra.hitDie : 'd10',
    primaryAbility: typeof extra.primaryAbility === 'string' ? extra.primaryAbility : '',
    featuresText: features.join('\n'),
    imageUrl: typeof extra.imageUrl === 'string' ? extra.imageUrl : '',
    abilityBonus: typeof extra.abilityBonus === 'string' ? extra.abilityBonus : '',
    speed: typeof extra.speed === 'number' ? extra.speed : 30,
    text: typeof extra.text === 'string' ? extra.text : '',
  })
}

function buildExtra(): Record<string, unknown> | null {
  if (activeKind.value === 'class') {
    return {
      description: form.description,
      hitDie: form.hitDie,
      primaryAbility: form.primaryAbility,
      features: form.featuresText
        .split('\n')
        .map((line) => line.trim())
        .filter(Boolean),
      imageUrl: form.imageUrl.trim(),
    }
  }

  if (activeKind.value === 'race') {
    return {
      description: form.description,
      abilityBonus: form.abilityBonus,
      speed: Number(form.speed) || 30,
    }
  }

  if (activeKind.value === 'inspiration') {
    return { text: form.text }
  }

  return null
}

function extraPreview(entry: AdminGeneratorEntry): string {
  const extra = (entry.extra ?? {}) as Record<string, unknown>
  if (entry.kind === 'class') {
    return [extra.hitDie, extra.primaryAbility].filter(Boolean).join(' · ')
  }
  if (entry.kind === 'race') {
    return [extra.abilityBonus, extra.speed === undefined ? '' : `速度 ${extra.speed}`].filter(Boolean).join(' · ')
  }
  if (entry.kind === 'inspiration') {
    return typeof extra.text === 'string' ? extra.text : ''
  }
  return ''
}

function previewImage(entry: AdminGeneratorEntry): string {
  const extra = (entry.extra ?? {}) as Record<string, unknown>
  return entry.kind === 'class' ? resolveAssetUrl(typeof extra.imageUrl === 'string' ? extra.imageUrl : '') : ''
}

async function uploadImage(options: UploadRequestOptions): Promise<void> {
  const body = new FormData()
  body.append('file', options.file)
  try {
    const { data } = await http.post<{ url: string }>('/api/admin/assets', body)
    form.imageUrl = data.url
    ElMessage.success('图片已上传，保存后生效')
  } catch (error) {
    ElMessage.error(errorMessage(error))
  }
}

async function loadSystems(): Promise<void> {
  const { data } = await http.get<AdminSystem[]>('/api/admin/systems')
  systems.value = data
}

async function load(): Promise<void> {
  loading.value = true
  try {
    const params: Record<string, unknown> = {
      kind: activeKind.value,
      page: page.value,
      pageSize: pageSize.value,
    }
    if (filters.systemCode) {
      params.systemCode = filters.systemCode
    }
    if (filters.keyword.trim()) {
      params.keyword = filters.keyword.trim()
    }
    if (filters.enabled !== '') {
      params.enabled = filters.enabled === 'true'
    }

    const { data } = await http.get<PagedResult<AdminGeneratorEntry>>('/api/admin/generator', { params })
    entries.value = data.items
    total.value = data.total
  } catch (error) {
    ElMessage.error(errorMessage(error))
  } finally {
    loading.value = false
  }
}

function onKindChange(): void {
  page.value = 1
  filters.keyword = ''
  void load()
}

function openCreate(): void {
  editingId.value = null
  Object.assign(form, emptyForm(), {
    systemCode: filters.systemCode,
    category: currentKind.value.defaultCategory,
  })
  dialogVisible.value = true
}

function openEdit(row: AdminGeneratorEntry): void {
  editingId.value = row.id
  fillForm(row)
  dialogVisible.value = true
}

function toPayload(overrides: Partial<SaveGeneratorEntryPayload> = {}): SaveGeneratorEntryPayload {
  return {
    systemCode: form.systemCode || null,
    kind: activeKind.value,
    category: form.category.trim(),
    value: form.value.trim(),
    extra: buildExtra(),
    sortOrder: Number(form.sortOrder) || 0,
    isEnabled: form.isEnabled,
    ...overrides,
  }
}

async function save(): Promise<void> {
  if (!form.category.trim() || !form.value.trim()) {
    ElMessage.warning('分类和内容不能为空')
    return
  }

  saving.value = true
  try {
    const payload = toPayload()
    if (editingId.value) {
      await http.put(`/api/admin/generator/${editingId.value}`, payload)
    } else {
      await http.post('/api/admin/generator', payload)
    }

    ElMessage.success('已保存')
    dialogVisible.value = false
    await load()
  } catch (error) {
    ElMessage.error(errorMessage(error))
  } finally {
    saving.value = false
  }
}

async function toggleEnabled(row: AdminGeneratorEntry): Promise<void> {
  try {
    await http.put(`/api/admin/generator/${row.id}`, {
      systemCode: row.systemCode ?? null,
      kind: row.kind,
      category: row.category,
      value: row.value,
      extra: row.extra ?? null,
      sortOrder: row.sortOrder,
      isEnabled: row.isEnabled,
    })
    ElMessage.success(row.isEnabled ? '已启用' : '已停用')
  } catch (error) {
    row.isEnabled = !row.isEnabled
    ElMessage.error(errorMessage(error))
  }
}

async function remove(row: AdminGeneratorEntry): Promise<void> {
  try {
    await ElMessageBox.confirm(`删除素材「${row.value}」？`, '确认删除', { type: 'warning' })
  } catch {
    return
  }

  try {
    await http.delete(`/api/admin/generator/${row.id}`)
    ElMessage.success('已删除')
    await load()
  } catch (error) {
    ElMessage.error(errorMessage(error))
  }
}

onMounted(async () => {
  try {
    await loadSystems()
  } catch (error) {
    ElMessage.error(errorMessage(error))
  }
  await load()
})
</script>

<template>
  <div>
    <div class="page-title">
      <h2>素材库</h2>
      <el-button size="small" @click="load">刷新</el-button>
      <span class="grow" />
      <el-button type="primary" @click="openCreate">新增素材</el-button>
    </div>

    <el-alert type="info" :closable="false" style="margin-bottom: 16px" :title="currentKind.hint" />

    <el-tabs v-model="activeKind" @tab-change="onKindChange">
      <el-tab-pane v-for="kind in kinds" :key="kind.key" :name="kind.key" :label="kind.label" />
    </el-tabs>

    <div class="toolbar">
      <el-select v-model="filters.systemCode" clearable placeholder="全部规则系统" style="width: 180px" @change="() => { page = 1; load() }">
        <el-option v-for="system in systems" :key="system.code" :label="`${system.name}（${system.code}）`" :value="system.code" />
      </el-select>
      <el-input
        v-model="filters.keyword"
        placeholder="搜索内容 / 分类"
        style="width: 220px"
        clearable
        @keyup.enter="() => { page = 1; load() }"
        @clear="() => { page = 1; load() }"
      />
      <el-select v-model="filters.enabled" placeholder="启停" style="width: 120px" @change="() => { page = 1; load() }">
        <el-option label="全部" value="" />
        <el-option label="启用" value="true" />
        <el-option label="停用" value="false" />
      </el-select>
      <el-button @click="() => { page = 1; load() }">查询</el-button>
      <span class="muted">共 {{ total }} 条</span>
    </div>

    <el-table v-loading="loading" :data="entries" border size="small">
      <el-table-column label="规则系统" width="120">
        <template #default="{ row }">{{ row.systemCode ?? '全局' }}</template>
      </el-table-column>
      <el-table-column v-if="activeKind === 'class'" label="配图" width="90">
        <template #default="{ row }">
          <el-image
            v-if="previewImage(row)"
            :src="previewImage(row)"
            fit="cover"
            style="width: 60px; height: 34px; border-radius: 4px"
          />
          <span v-else class="muted">—</span>
        </template>
      </el-table-column>
      <el-table-column prop="category" label="分类" width="140" />
      <el-table-column prop="value" label="内容" min-width="200" />
      <el-table-column label="扩展信息" min-width="220">
        <template #default="{ row }">
          <span class="muted">{{ extraPreview(row) || '—' }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="sortOrder" label="排序" width="80" />
      <el-table-column label="启用" width="90">
        <template #default="{ row }">
          <el-switch v-model="row.isEnabled" @change="toggleEnabled(row)" />
        </template>
      </el-table-column>
      <el-table-column label="操作" width="130" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" @click="openEdit(row)">编辑</el-button>
          <el-button link type="danger" @click="remove(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-pagination
      v-model:current-page="page"
      v-model:page-size="pageSize"
      style="margin-top: 16px; justify-content: flex-end"
      layout="total, sizes, prev, pager, next"
      :total="total"
      :page-sizes="[20, 50, 100]"
      @current-change="load"
      @size-change="() => { page = 1; load() }"
    />

    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑素材' : `新增素材 · ${currentKind.label}`" width="640px">
      <el-form label-position="top">
        <div class="form-grid">
          <el-form-item label="规则系统">
            <el-select v-model="form.systemCode" clearable placeholder="全局（所有系统）">
              <el-option v-for="system in systems" :key="system.code" :label="system.code" :value="system.code" />
            </el-select>
          </el-form-item>
          <el-form-item :label="`分类（${currentKind.categoryHint}）`">
            <el-input v-model="form.category" :placeholder="currentKind.categoryHint" />
          </el-form-item>
          <el-form-item :label="activeKind === 'inspiration' ? '标题' : '内容'">
            <el-input v-model="form.value" />
          </el-form-item>
          <el-form-item label="排序">
            <el-input-number v-model="form.sortOrder" :min="0" :step="10" />
          </el-form-item>
        </div>

        <el-form-item label="启用">
          <el-switch v-model="form.isEnabled" />
        </el-form-item>

        <template v-if="hasExtraFields">
          <el-divider content-position="left">扩展信息</el-divider>

          <el-form-item v-if="activeKind !== 'inspiration'" label="描述">
            <el-input v-model="form.description" type="textarea" :rows="2" />
          </el-form-item>

          <template v-if="activeKind === 'class'">
            <div class="form-grid">
              <el-form-item label="生命骰">
                <el-input v-model="form.hitDie" placeholder="d10" />
              </el-form-item>
              <el-form-item label="主属性">
                <el-input v-model="form.primaryAbility" placeholder="敏捷 / 感知" />
              </el-form-item>
            </div>
            <el-form-item label="职业配图（上传或直接填地址）">
              <div class="image-row">
                <el-input v-model="form.imageUrl" placeholder="/static/classes/ranger.jpg 或 https://..." />
                <el-upload :show-file-list="false" accept="image/*" :http-request="uploadImage">
                  <el-button>上传图片</el-button>
                </el-upload>
                <el-image
                  v-if="form.imageUrl"
                  :src="resolveAssetUrl(form.imageUrl)"
                  fit="cover"
                  style="width: 96px; height: 54px; border-radius: 6px; flex-shrink: 0"
                />
              </div>
            </el-form-item>
            <el-form-item label="特性（每行一条）">
              <el-input v-model="form.featuresText" type="textarea" :rows="5" />
            </el-form-item>
          </template>

          <div v-if="activeKind === 'race'" class="form-grid">
            <el-form-item label="属性加值">
              <el-input v-model="form.abilityBonus" placeholder="敏捷 +2" />
            </el-form-item>
            <el-form-item label="速度">
              <el-input-number v-model="form.speed" :min="0" :step="5" />
            </el-form-item>
          </div>

          <el-form-item v-if="activeKind === 'inspiration'" label="正文">
            <el-input v-model="form.text" type="textarea" :rows="4" />
          </el-form-item>
        </template>
      </el-form>

      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="save">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>
