<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { UploadRequestOptions } from 'element-plus'
import { errorMessage, http, resolveAssetUrl } from '../api/client'
import type {
  AdminClassLevelFeature,
  AdminCreationStep,
  AdminGeneratorEntry,
  AdminSystem,
  CreationFlowPreview,
  CreationStepKindKey,
  PagedResult,
  SaveClassLevelItem,
  SaveCreationStepPayload,
} from '../api/types'

interface StepKindOption {
  key: CreationStepKindKey
  label: string
  hint: string
}

const kindOptions: StepKindOption[] = [
  { key: 'class', label: '选择职业', hint: '选职业卡（已实现面板）' },
  { key: 'subclass', label: '选择子职', hint: '选子职卡，需要配图（圣武士誓约已实现）' },
  { key: 'race', label: '选择种族', hint: '种族卡列表，数据来自 种族.xlsx（已实现）' },
  { key: 'subrace', label: '选择亚种', hint: '亚种卡列表，按种族过滤，数据来自 亚种.xlsx（已实现）' },
  { key: 'background', label: '决定背景', hint: '第一步是九宫格阵营，数据来自 阵营.xlsx（已实现）' },
  { key: 'attributes', label: '决定属性', hint: '掷骰 / 标准数组 / 手动填，点每项看来源，依赖决定背景（已实现）' },
  { key: 'name', label: '起名', hint: '卡名与背景（占位，待实现）' },
  { key: 'summary', label: '确认卡面', hint: '提交前总览（占位，待实现）' },
]

/* ---------------- 流程步骤 ---------------- */

const steps = ref<AdminCreationStep[]>([])
const systems = ref<AdminSystem[]>([])
const loading = ref(false)
const saving = ref(false)
const dialogVisible = ref(false)
const editingId = ref<string | null>(null)
const previewVisible = ref(false)
const preview = ref<CreationFlowPreview | null>(null)
const previewSystemCode = ref('dnd5e')

const form = reactive({
  systemCode: 'dnd5e',
  kind: 'class' as CreationStepKindKey,
  title: '',
  description: '',
  sortOrder: 10,
  isEnabled: true,
})

const currentKind = computed(() => kindOptions.find((item) => item.key === form.kind) ?? kindOptions[0])

/* ---------------- 职业卡（第一步的内容） ---------------- */

const classDrawerVisible = ref(false)
const classDialogVisible = ref(false)
const classLoading = ref(false)
const classSaving = ref(false)
const classSystemCode = ref('dnd5e')
const classEntries = ref<AdminGeneratorEntry[]>([])
const editingClass = ref<AdminGeneratorEntry | null>(null)

const classForm = reactive({
  value: '',
  category: '',
  description: '',
  imageUrl: '',
  hitDie: 'd10',
  primaryAbility: '',
  savingThrows: '',
  spellcastingAbility: '',
  featuresText: '',
  isEnabled: true,
  sortOrder: 10,
})

const previewFeatures = computed(() =>
  classForm.featuresText
    .split('\n')
    .map((line) => line.trim())
    .filter(Boolean),
)

const levelsVisible = ref(false)
const levelsLoading = ref(false)
const levelsSaving = ref(false)
const levelsClassCode = ref('')
const levelsClassName = ref('')
const levelItems = ref<SaveClassLevelItem[]>([])

const previewImage = computed(() => resolveAssetUrl(classForm.imageUrl))

function resetForm(step?: AdminCreationStep): void {
  form.systemCode = step?.systemCode ?? 'dnd5e'
  form.kind = step?.kind ?? 'class'
  form.title = step?.title ?? ''
  form.description = step?.description ?? ''
  form.sortOrder = step?.sortOrder ?? 10
  form.isEnabled = step?.isEnabled ?? true
}

async function loadSystems(): Promise<void> {
  const { data } = await http.get<AdminSystem[]>('/api/admin/systems')
  systems.value = data
}

async function load(): Promise<void> {
  loading.value = true
  try {
    const { data } = await http.get<AdminCreationStep[]>('/api/admin/creation-steps')
    steps.value = data
  } catch (error) {
    ElMessage.error(errorMessage(error))
  } finally {
    loading.value = false
  }
}

function openCreate(): void {
  editingId.value = null
  resetForm()
  dialogVisible.value = true
}

function openEdit(step: AdminCreationStep): void {
  editingId.value = step.id
  resetForm(step)
  dialogVisible.value = true
}

async function save(): Promise<void> {
  if (!form.title.trim()) {
    ElMessage.warning('标题不能为空')
    return
  }

  const payload: SaveCreationStepPayload = {
    systemCode: form.systemCode || null,
    kind: form.kind,
    title: form.title.trim(),
    description: form.description.trim() || null,
    sortOrder: Number(form.sortOrder) || 0,
    isEnabled: form.isEnabled,
  }

  saving.value = true
  try {
    if (editingId.value) {
      await http.put(`/api/admin/creation-steps/${editingId.value}`, payload)
    } else {
      await http.post('/api/admin/creation-steps', payload)
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

async function remove(step: AdminCreationStep): Promise<void> {
  try {
    await ElMessageBox.confirm(`删除步骤「${step.title}」？`, '确认删除', { type: 'warning' })
  } catch {
    return
  }

  try {
    await http.delete(`/api/admin/creation-steps/${step.id}`)
    ElMessage.success('已删除')
    await load()
  } catch (error) {
    ElMessage.error(errorMessage(error))
  }
}

async function openPreview(): Promise<void> {
  try {
    const { data } = await http.get<CreationFlowPreview>('/api/creation-flow', {
      params: { systemCode: previewSystemCode.value },
    })
    preview.value = data
    previewVisible.value = true
  } catch (error) {
    ElMessage.error(errorMessage(error))
  }
}

function kindLabel(kind: CreationStepKindKey): string {
  return kindOptions.find((item) => item.key === kind)?.label ?? kind
}

/* ---------------- 职业卡编辑 ---------------- */

function classImage(entry: AdminGeneratorEntry): string {
  const extra = (entry.extra ?? {}) as Record<string, unknown>
  return resolveAssetUrl(typeof extra.imageUrl === 'string' ? extra.imageUrl : '')
}

function classDescription(entry: AdminGeneratorEntry): string {
  const extra = (entry.extra ?? {}) as Record<string, unknown>
  return typeof extra.description === 'string' ? extra.description : ''
}

async function openClasses(step: AdminCreationStep): Promise<void> {
  classSystemCode.value = step.systemCode || 'dnd5e'
  classDrawerVisible.value = true
  await loadClasses()
}

async function loadClasses(): Promise<void> {
  classLoading.value = true
  try {
    const { data } = await http.get<PagedResult<AdminGeneratorEntry>>('/api/admin/generator', {
      params: { kind: 'class', systemCode: classSystemCode.value, pageSize: 200 },
    })
    classEntries.value = data.items
  } catch (error) {
    ElMessage.error(errorMessage(error))
  } finally {
    classLoading.value = false
  }
}

function openClassEditor(entry: AdminGeneratorEntry): void {
  const extra = (entry.extra ?? {}) as Record<string, unknown>
  const features = Array.isArray(extra.features) ? (extra.features as string[]) : []

  editingClass.value = entry
  Object.assign(classForm, {
    value: entry.value,
    category: entry.category,
    description: typeof extra.description === 'string' ? extra.description : '',
    imageUrl: typeof extra.imageUrl === 'string' ? extra.imageUrl : '',
    hitDie: typeof extra.hitDie === 'string' ? extra.hitDie : 'd10',
    primaryAbility: typeof extra.primaryAbility === 'string' ? extra.primaryAbility : '',
    savingThrows: typeof extra.savingThrows === 'string' ? extra.savingThrows : '',
    spellcastingAbility: typeof extra.spellcastingAbility === 'string' ? extra.spellcastingAbility : '',
    featuresText: features.join('\n'),
    isEnabled: entry.isEnabled,
    sortOrder: entry.sortOrder,
  })
  classDialogVisible.value = true
}

function openClassCreate(): void {
  editingClass.value = null
  const nextSort = classEntries.value.reduce((max, entry) => Math.max(max, entry.sortOrder), 0) + 10
  Object.assign(classForm, {
    value: '',
    category: '',
    description: '',
    imageUrl: '',
    hitDie: 'd10',
    primaryAbility: '',
    savingThrows: '',
    spellcastingAbility: '',
    featuresText: '',
    isEnabled: true,
    sortOrder: nextSort,
  })
  classDialogVisible.value = true
}

async function deleteClass(entry: AdminGeneratorEntry): Promise<void> {
  try {
    await ElMessageBox.confirm(`删除职业卡「${entry.value}」？已经车好的卡不受影响。`, '确认删除', { type: 'warning' })
  } catch {
    return
  }

  try {
    await http.delete(`/api/admin/generator/${entry.id}`)
    ElMessage.success('已删除')
    await loadClasses()
  } catch (error) {
    ElMessage.error(errorMessage(error))
  }
}

/* ---------------- 等级成长 ---------------- */

async function openLevels(entry: AdminGeneratorEntry): Promise<void> {
  levelsClassCode.value = entry.category
  levelsClassName.value = entry.value
  levelsVisible.value = true
  await loadLevels()
}

async function loadLevels(): Promise<void> {
  levelsLoading.value = true
  try {
    const { data } = await http.get<AdminClassLevelFeature[]>(
      `/api/admin/classes/${encodeURIComponent(levelsClassCode.value)}/levels`,
      { params: { systemCode: classSystemCode.value } },
    )
    levelItems.value = data.map((item) => ({
      level: item.level,
      title: item.title,
      description: item.description,
      sortOrder: item.sortOrder,
      isEnabled: item.isEnabled,
    }))
  } catch (error) {
    ElMessage.error(errorMessage(error))
  } finally {
    levelsLoading.value = false
  }
}

function addLevelRow(): void {
  const maxLevel = levelItems.value.reduce((max, item) => Math.max(max, item.level), 0)
  levelItems.value.push({
    level: Math.min(20, maxLevel + 1 || 1),
    title: '',
    description: '',
    sortOrder: (levelItems.value.length + 1) * 10,
    isEnabled: true,
  })
}

function removeLevelRow(index: number): void {
  levelItems.value.splice(index, 1)
}

async function saveLevels(): Promise<void> {
  const invalid = levelItems.value.find((item) => !item.title.trim() || !item.description.trim())
  if (invalid) {
    ElMessage.warning('特性名和说明都不能为空')
    return
  }

  levelsSaving.value = true
  try {
    await http.put(`/api/admin/classes/${encodeURIComponent(levelsClassCode.value)}/levels`, {
      systemCode: classSystemCode.value,
      items: levelItems.value.map((item) => ({
        level: Number(item.level) || 1,
        title: item.title.trim(),
        description: item.description.trim(),
        sortOrder: Number(item.sortOrder) || 0,
        isEnabled: item.isEnabled,
      })),
    })
    ElMessage.success('已保存')
    levelsVisible.value = false
  } catch (error) {
    ElMessage.error(errorMessage(error))
  } finally {
    levelsSaving.value = false
  }
}

async function uploadClassImage(options: UploadRequestOptions): Promise<void> {
  const body = new FormData()
  body.append('file', options.file)
  try {
    const { data } = await http.post<{ url: string }>('/api/admin/assets', body)
    classForm.imageUrl = data.url
    ElMessage.success('图片已上传，保存后生效')
  } catch (error) {
    ElMessage.error(errorMessage(error))
  }
}

async function saveClass(): Promise<void> {
  if (!classForm.value.trim()) {
    ElMessage.warning('职业名不能为空')
    return
  }

  if (!classForm.category.trim()) {
    ElMessage.warning('分类（稳定 code）不能为空，例如 ranger')
    return
  }

  const payload = {
    systemCode: classSystemCode.value,
    kind: 'class',
    category: classForm.category.trim(),
    value: classForm.value.trim(),
    extra: {
      description: classForm.description,
      hitDie: classForm.hitDie,
      primaryAbility: classForm.primaryAbility,
      savingThrows: classForm.savingThrows,
      spellcastingAbility: classForm.spellcastingAbility,
      features: previewFeatures.value,
      imageUrl: classForm.imageUrl.trim(),
    },
    sortOrder: Number(classForm.sortOrder) || 0,
    isEnabled: classForm.isEnabled,
  }

  classSaving.value = true
  try {
    if (editingClass.value) {
      await http.put(`/api/admin/generator/${editingClass.value.id}`, payload)
    } else {
      await http.post('/api/admin/generator', payload)
    }

    ElMessage.success('已保存')
    classDialogVisible.value = false
    await loadClasses()
  } catch (error) {
    ElMessage.error(errorMessage(error))
  } finally {
    classSaving.value = false
  }
}

/** 关键词：文本里 [方括号] 包起来的部分，前端显示为斜体 + 粗体。 */
function splitKeywords(text?: string | null): Array<{ text: string; keyword: boolean }> {
  const source = text ?? ''
  const pattern = /\[([^\]]+)\]/g
  const segments: Array<{ text: string; keyword: boolean }> = []
  let cursor = 0
  let match: RegExpExecArray | null

  while ((match = pattern.exec(source)) !== null) {
    if (match.index > cursor) {
      segments.push({ text: source.slice(cursor, match.index), keyword: false })
    }
    segments.push({ text: match[1], keyword: true })
    cursor = match.index + match[0].length
  }

  if (cursor < source.length) {
    segments.push({ text: source.slice(cursor), keyword: false })
  }

  return segments.filter((segment) => segment.text.length > 0)
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
  <div v-loading="loading">
    <div class="page-title">
      <h2>车卡流程</h2>
      <el-button size="small" @click="load">刷新</el-button>
      <span class="grow" />
      <el-button @click="openPreview">预览前端流程</el-button>
      <el-button type="primary" @click="openCreate">新增步骤</el-button>
    </div>

    <el-alert
      type="info"
      :closable="false"
      style="margin-bottom: 16px"
      title="选择规则书（DND 5E / COC 7E）是车卡的前置条件，不算步骤；「选择职业」这一步里的职业卡可以单独编辑描述和配图。"
    />

    <el-table :data="steps" border size="small">
      <el-table-column label="规则系统" width="120">
        <template #default="{ row }">{{ row.systemCode ?? '通用' }}</template>
      </el-table-column>
      <el-table-column label="步骤类型" width="140">
        <template #default="{ row }">{{ kindLabel(row.kind) }}</template>
      </el-table-column>
      <el-table-column prop="title" label="标题" width="180" />
      <el-table-column prop="description" label="说明" min-width="200">
        <template #default="{ row }">
          <span class="muted">{{ row.description || '—' }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="sortOrder" label="排序" width="80" />
      <el-table-column label="启用" width="90">
        <template #default="{ row }">
          <el-tag :type="row.isEnabled ? 'success' : 'info'" size="small">
            {{ row.isEnabled ? '启用' : '停用' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="220" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" @click="openEdit(row)">编辑步骤</el-button>
          <el-button v-if="row.kind === 'class'" link type="primary" @click="openClasses(row)">编辑职业</el-button>
          <el-button link type="danger" @click="remove(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- 步骤编辑 -->
    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑步骤' : '新增步骤'" width="560px">
      <el-form label-position="top">
        <div class="form-grid">
          <el-form-item label="规则系统">
            <el-select v-model="form.systemCode" clearable placeholder="通用（所有系统）">
              <el-option v-for="system in systems" :key="system.code" :label="system.code" :value="system.code" />
            </el-select>
          </el-form-item>
          <el-form-item label="步骤类型">
            <el-select v-model="form.kind">
              <el-option v-for="item in kindOptions" :key="item.key" :label="item.label" :value="item.key" />
            </el-select>
          </el-form-item>
          <el-form-item label="标题">
            <el-input v-model="form.title" placeholder="选择职业" />
          </el-form-item>
          <el-form-item label="排序">
            <el-input-number v-model="form.sortOrder" :min="0" :step="10" />
          </el-form-item>
        </div>

        <el-form-item label="说明（显示在步骤面板顶部）">
          <el-input v-model="form.description" type="textarea" :rows="2" />
        </el-form-item>

        <el-form-item label="启用">
          <el-switch v-model="form.isEnabled" />
        </el-form-item>

        <el-alert type="warning" :closable="false" :title="`当前类型：${currentKind.label} —— ${currentKind.hint}`" />
      </el-form>

      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="save">保存</el-button>
      </template>
    </el-dialog>

    <!-- 职业卡列表 -->
    <el-drawer v-model="classDrawerVisible" :title="`职业卡配置（${classSystemCode}）`" size="820px">
      <div class="toolbar">
        <el-button size="small" @click="loadClasses">刷新</el-button>
        <el-button type="primary" size="small" @click="openClassCreate">新增职业卡</el-button>
        <span class="muted">点「编辑」改描述和配图，右侧实时预览。</span>
      </div>

      <el-table v-loading="classLoading" :data="classEntries" border size="small">
        <el-table-column label="配图" width="110">
          <template #default="{ row }">
            <el-image
              v-if="classImage(row)"
              :src="classImage(row)"
              fit="cover"
              style="width: 84px; height: 48px; border-radius: 6px; background: #2b211a"
            />
            <span v-else class="muted">—</span>
          </template>
        </el-table-column>
        <el-table-column prop="value" label="职业" width="110" />
        <el-table-column prop="category" label="分类" width="100" />
        <el-table-column label="描述" min-width="220">
          <template #default="{ row }">
            <span class="muted">{{ classDescription(row) || '—' }}</span>
          </template>
        </el-table-column>
        <el-table-column label="操作" width="200">
          <template #default="{ row }">
            <el-button link type="primary" @click="openClassEditor(row)">编辑</el-button>
            <el-button link type="primary" @click="openLevels(row)">成长</el-button>
            <el-button link type="danger" @click="deleteClass(row)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <el-empty v-if="!classLoading && !classEntries.length" description="这个规则系统还没有职业卡" />
    </el-drawer>

    <!-- 职业卡编辑：左边表单，右边实时预览 -->
    <el-dialog v-model="classDialogVisible" :title="editingClass ? '编辑职业卡' : '新增职业卡'" width="900px" top="6vh">
      <div class="class-editor">
        <div class="class-editor-form">
          <el-form label-position="top">
            <div class="form-grid">
              <el-form-item label="职业名">
                <el-input v-model="classForm.value" placeholder="游侠" />
              </el-form-item>
              <el-form-item label="分类（稳定 code）">
                <el-input v-model="classForm.category" placeholder="ranger" />
              </el-form-item>
            </div>

            <el-form-item label="描述（显示在职业卡上）">
              <el-input v-model="classForm.description" type="textarea" :rows="3" />
              <span class="muted" style="font-size: 12px">
                用 [方括号] 包起来的是关键词，前端显示为斜体 + 粗体。
              </span>
            </el-form-item>

            <el-form-item label="配图">
              <div class="image-row">
                <el-input v-model="classForm.imageUrl" placeholder="/static/classes/ranger.jpg 或 https://..." />
                <el-upload :show-file-list="false" accept="image/*" :http-request="uploadClassImage">
                  <el-button>上传图片</el-button>
                </el-upload>
              </div>
              <span class="muted" style="font-size: 12px">
                相对路径按前端地址解析（默认 http://localhost:5173）；上传后会变成 /uploads/... 的绝对地址。
              </span>
            </el-form-item>

            <div class="form-grid">
              <el-form-item label="生命骰">
                <el-input v-model="classForm.hitDie" placeholder="d10" />
              </el-form-item>
              <el-form-item label="主属性">
                <el-input v-model="classForm.primaryAbility" placeholder="敏捷 / 感知" />
              </el-form-item>
            </div>

            <div class="form-grid">
              <el-form-item label="豁免熟练（逗号分隔）">
                <el-input v-model="classForm.savingThrows" placeholder="力量,体质" />
              </el-form-item>
              <el-form-item label="1 级施法属性（空 = 不施法）">
                <el-input v-model="classForm.spellcastingAbility" placeholder="魅力" />
              </el-form-item>
            </div>

            <el-form-item label="特性（每行一条）">
              <el-input v-model="classForm.featuresText" type="textarea" :rows="4" />
            </el-form-item>

            <div class="form-grid">
              <el-form-item label="排序">
                <el-input-number v-model="classForm.sortOrder" :min="0" :step="10" />
              </el-form-item>
              <el-form-item label="启用">
                <el-switch v-model="classForm.isEnabled" />
              </el-form-item>
            </div>
          </el-form>
        </div>

        <div class="class-editor-preview">
          <div class="preview-title muted">前端预览</div>
          <div class="card-preview">
            <div class="card-cover">
              <img v-if="previewImage" :src="previewImage" alt="职业配图" />
              <div v-else class="card-cover-fallback">{{ (classForm.value || '职')[0] }}</div>
            </div>
            <div class="card-body">
              <div class="card-name">{{ classForm.value || '职业名' }}</div>
              <div class="card-sub">
                生命骰
                <span
                  v-for="(segment, index) in splitKeywords(classForm.hitDie || '-')"
                  :key="`h-${index}`"
                  :class="{ kw: segment.keyword }"
                >{{ segment.text }}</span>
                · 主要属性
                <span
                  v-for="(segment, index) in splitKeywords(classForm.primaryAbility || '-')"
                  :key="`p-${index}`"
                  :class="{ kw: segment.keyword }"
                >{{ segment.text }}</span>
              </div>
              <div class="card-desc">
                <template v-if="classForm.description">
                  <span
                    v-for="(segment, index) in splitKeywords(classForm.description)"
                    :key="index"
                    :class="{ kw: segment.keyword }"
                  >{{ segment.text }}</span>
                </template>
                <span v-else>这里是职业描述，前端会显示在职业卡上。</span>
              </div>
              <div class="card-features">
                <div v-for="(feature, index) in previewFeatures" :key="index" class="card-feature">
                  ◆
                  <span
                    v-for="(segment, segIndex) in splitKeywords(feature)"
                    :key="segIndex"
                    :class="{ kw: segment.keyword }"
                  >{{ segment.text }}</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <template #footer>
        <el-button @click="classDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="classSaving" @click="saveClass">保存</el-button>
      </template>
    </el-dialog>

    <!-- 职业等级成长 -->
    <el-dialog
      v-model="levelsVisible"
      :title="`等级成长 · ${levelsClassName}（${levelsClassCode}）`"
      width="980px"
      top="5vh"
    >
      <div class="toolbar">
        <el-button size="small" @click="loadLevels">刷新</el-button>
        <el-button size="small" type="primary" @click="addLevelRow">添加一行</el-button>
        <span class="muted">按 DND 5E 基础职业特性维护，等级 1-20；说明里可以用 [关键词]。</span>
      </div>

      <el-table v-loading="levelsLoading" :data="levelItems" border size="small" max-height="460">
        <el-table-column label="等级" width="88">
          <template #default="{ row }">
            <el-input-number v-model="row.level" :min="1" :max="20" size="small" controls-position="right" />
          </template>
        </el-table-column>
        <el-table-column label="特性名" width="190">
          <template #default="{ row }">
            <el-input v-model="row.title" size="small" />
          </template>
        </el-table-column>
        <el-table-column label="说明">
          <template #default="{ row }">
            <el-input v-model="row.description" type="textarea" :rows="2" size="small" />
          </template>
        </el-table-column>
        <el-table-column label="排序" width="88">
          <template #default="{ row }">
            <el-input-number v-model="row.sortOrder" :min="0" :step="10" size="small" controls-position="right" />
          </template>
        </el-table-column>
        <el-table-column label="启用" width="70">
          <template #default="{ row }">
            <el-switch v-model="row.isEnabled" />
          </template>
        </el-table-column>
        <el-table-column label="操作" width="70">
          <template #default="{ $index }">
            <el-button link type="danger" @click="removeLevelRow($index)">删除</el-button>
          </template>
        </el-table-column>
      </el-table>

      <template #footer>
        <el-button @click="levelsVisible = false">取消</el-button>
        <el-button type="primary" :loading="levelsSaving" @click="saveLevels">保存</el-button>
      </template>
    </el-dialog>

    <!-- 前端流程预览 -->
    <el-drawer v-model="previewVisible" title="前端车卡流程预览" size="420px">
      <el-select v-model="previewSystemCode" style="width: 100%; margin-bottom: 16px" @change="openPreview">
        <el-option v-for="system in systems" :key="system.code" :label="`${system.name}（${system.code}）`" :value="system.code" />
      </el-select>

      <el-alert type="info" :closable="false" style="margin-bottom: 16px" title="规则书选择在流程之前，不计入步骤" />

      <el-steps v-if="preview?.steps.length" direction="vertical" :active="preview.steps.length">
        <el-step
          v-for="(step, index) in preview.steps"
          :key="step.id"
          :title="`${index + 1}. ${step.title}`"
          :description="step.description || kindLabel(step.kind)"
        />
      </el-steps>
      <el-empty v-else description="这个系统还没有配置车卡步骤" />
    </el-drawer>
  </div>
</template>
