<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { UploadRequestOptions } from 'element-plus'
import { errorMessage, http, resolveAssetUrl } from '../api/client'
import type { AdminKeyword, SaveKeywordPayload } from '../api/types'

const keywords = ref<AdminKeyword[]>([])
const loading = ref(false)
const saving = ref(false)
const dialogVisible = ref(false)
const editingId = ref<string | null>(null)
const search = ref('')

const form = reactive({
  key: '',
  title: '',
  content: '',
  imageUrl: '',
  sortOrder: 10,
  isEnabled: true,
})

const imagePreview = computed(() => resolveAssetUrl(form.imageUrl))
const contentSegments = computed(() => splitKeywords(form.content))
const knownKeys = computed(() => new Set(keywords.value.map((item) => item.key.trim().toLowerCase())))

/** 正文里引用了、但词条库里还没有的关键词，保存后前端会原样显示 [xxx]。 */
const missingReferences = computed(() => {
  const missing: string[] = []
  for (const segment of contentSegments.value) {
    if (!segment.keyword) {
      continue
    }

    const key = segment.text.trim().toLowerCase()
    if (key && key !== form.key.trim().toLowerCase() && !knownKeys.value.has(key)) {
      missing.push(segment.text)
    }
  }

  return Array.from(new Set(missing))
})

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

async function load(): Promise<void> {
  loading.value = true
  try {
    const { data } = await http.get<AdminKeyword[]>('/api/admin/keywords', {
      params: search.value.trim() ? { keyword: search.value.trim() } : {},
    })
    keywords.value = data
  } catch (error) {
    ElMessage.error(errorMessage(error))
  } finally {
    loading.value = false
  }
}

function resetForm(keyword?: AdminKeyword): void {
  form.key = keyword?.key ?? ''
  form.title = keyword?.title ?? ''
  form.content = keyword?.content ?? ''
  form.imageUrl = keyword?.imageUrl ?? ''
  form.sortOrder = keyword?.sortOrder ?? (keywords.value.length + 1) * 10
  form.isEnabled = keyword?.isEnabled ?? true
}

function openCreate(): void {
  editingId.value = null
  resetForm()
  dialogVisible.value = true
}

function openEdit(keyword: AdminKeyword): void {
  editingId.value = keyword.id
  resetForm(keyword)
  dialogVisible.value = true
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

async function save(): Promise<void> {
  if (!form.key.trim()) {
    ElMessage.warning('关键词不能为空')
    return
  }

  if (!form.content.trim()) {
    ElMessage.warning('解释正文不能为空')
    return
  }

  const payload: SaveKeywordPayload = {
    key: form.key.trim(),
    title: form.title.trim() || null,
    content: form.content.trim(),
    imageUrl: form.imageUrl.trim() || null,
    sortOrder: Number(form.sortOrder) || 0,
    isEnabled: form.isEnabled,
  }

  saving.value = true
  try {
    if (editingId.value) {
      await http.put(`/api/admin/keywords/${editingId.value}`, payload)
    } else {
      await http.post('/api/admin/keywords', payload)
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

async function remove(keyword: AdminKeyword): Promise<void> {
  try {
    await ElMessageBox.confirm(`删除关键词「${keyword.key}」？前端会恢复成原样显示 [${keyword.key}]。`, '确认删除', {
      type: 'warning',
    })
  } catch {
    return
  }

  try {
    await http.delete(`/api/admin/keywords/${keyword.id}`)
    ElMessage.success('已删除')
    await load()
  } catch (error) {
    ElMessage.error(errorMessage(error))
  }
}

onMounted(load)
</script>

<template>
  <div v-loading="loading">
    <div class="page-title">
      <h2>关键词</h2>
      <el-button size="small" @click="load">刷新</el-button>
      <span class="grow" />
      <el-input
        v-model="search"
        placeholder="搜索关键词 / 标题 / 正文"
        style="width: 240px"
        clearable
        @keyup.enter="load"
        @clear="load"
      />
      <el-button type="primary" @click="openCreate">新增关键词</el-button>
    </div>

    <el-alert
      type="info"
      :closable="false"
      style="margin-bottom: 16px"
      title="前端文本里用 [方括号] 包起来的就是关键词：有词条 → 显示为斜体加粗带下划线、可点击弹出解释；没有词条 → 原样显示 [方括号]。解释正文里还能继续嵌 [其它关键词]。"
    />

    <el-table :data="keywords" border size="small">
      <el-table-column prop="key" label="关键词" width="140" />
      <el-table-column prop="title" label="标题" width="160">
        <template #default="{ row }">
          <span>{{ row.title || '—' }}</span>
        </template>
      </el-table-column>
      <el-table-column label="配图" width="100">
        <template #default="{ row }">
          <el-image
            v-if="resolveAssetUrl(row.imageUrl)"
            :src="resolveAssetUrl(row.imageUrl)"
            fit="cover"
            style="width: 72px; height: 42px; border-radius: 6px; background: #2b211a"
          />
          <span v-else class="muted">—</span>
        </template>
      </el-table-column>
      <el-table-column prop="content" label="正文" min-width="280">
        <template #default="{ row }">
          <span class="muted">{{ row.content }}</span>
        </template>
      </el-table-column>
      <el-table-column prop="sortOrder" label="排序" width="80" />
      <el-table-column label="启用" width="80">
        <template #default="{ row }">
          <el-tag :type="row.isEnabled ? 'success' : 'info'" size="small">
            {{ row.isEnabled ? '启用' : '停用' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="130" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" @click="openEdit(row)">编辑</el-button>
          <el-button link type="danger" @click="remove(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑关键词' : '新增关键词'" width="900px" top="6vh">
      <div class="class-editor">
        <div class="class-editor-form">
          <el-form label-position="top">
            <div class="form-grid">
              <el-form-item label="关键词（不用写方括号）">
                <el-input v-model="form.key" placeholder="敏捷" />
              </el-form-item>
              <el-form-item label="标题（可留空，默认同关键词）">
                <el-input v-model="form.title" placeholder="敏捷" />
              </el-form-item>
            </div>

            <el-form-item label="解释正文（支持嵌套 [其它关键词]）">
              <el-input v-model="form.content" type="textarea" :rows="6" />
              <span v-if="missingReferences.length" class="muted" style="font-size: 12px">
                这些关键词还没有词条，前端会原样显示：{{ missingReferences.map((item) => `[${item}]`).join('、') }}
              </span>
            </el-form-item>

            <el-form-item label="配图">
              <div class="image-row">
                <el-input v-model="form.imageUrl" placeholder="/uploads/xxx.png 或 https://..." />
                <el-upload :show-file-list="false" accept="image/*" :http-request="uploadImage">
                  <el-button>上传图片</el-button>
                </el-upload>
              </div>
            </el-form-item>

            <div class="form-grid">
              <el-form-item label="排序">
                <el-input-number v-model="form.sortOrder" :min="0" :step="10" />
              </el-form-item>
              <el-form-item label="启用">
                <el-switch v-model="form.isEnabled" />
              </el-form-item>
            </div>
          </el-form>
        </div>

        <div class="class-editor-preview">
          <div class="preview-title muted">弹窗预览</div>
          <div class="card-preview keyword-preview">
            <div class="kw-preview-head">
              <span class="kw-preview-title">{{ form.title || form.key || '关键词' }}</span>
            </div>
            <img v-if="imagePreview" class="kw-preview-image" :src="imagePreview" alt="关键词配图" />
            <div class="kw-preview-body">
              <template v-if="form.content">
                <span
                  v-for="(segment, index) in contentSegments"
                  :key="index"
                  :class="{ kw: segment.keyword }"
                >{{ segment.text }}</span>
              </template>
              <span v-else class="muted">解释正文会显示在这里，正文里的 [关键词] 会变成可点击的链接。</span>
            </div>
            <div class="kw-preview-actions">
              <span class="kw-preview-btn">关闭</span>
              <span class="kw-preview-btn primary">关闭全部</span>
            </div>
          </div>
        </div>
      </div>

      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="save">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>
