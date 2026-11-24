<script setup lang="ts">
import { onMounted, reactive, ref } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { errorMessage, http } from '../api/client'
import type { AdminSystem, AttributeTemplate } from '../api/types'

interface SystemForm {
  code: string
  name: string
  description: string
  nameTemplate: string
  sortOrder: number
  attributes: AttributeTemplate[]
}

const systems = ref<AdminSystem[]>([])
const loading = ref(false)
const saving = ref(false)
const dialogVisible = ref(false)
const editingId = ref<string | null>(null)

const form = reactive<SystemForm>(emptyForm())

function emptyForm(): SystemForm {
  return {
    code: '',
    name: '',
    description: '',
    nameTemplate: '{surname}{given}',
    sortOrder: 10,
    attributes: [{ key: 'STR', label: '力量', formula: '3d6*5' }],
  }
}

function resetForm(source: SystemForm): void {
  form.code = source.code
  form.name = source.name
  form.description = source.description
  form.nameTemplate = source.nameTemplate
  form.sortOrder = source.sortOrder
  form.attributes = source.attributes.map((item) => ({ ...item }))
}

async function load(): Promise<void> {
  loading.value = true
  try {
    const { data } = await http.get<AdminSystem[]>('/api/admin/systems')
    systems.value = data
  } catch (error) {
    ElMessage.error(errorMessage(error))
  } finally {
    loading.value = false
  }
}

function openCreate(): void {
  editingId.value = null
  resetForm(emptyForm())
  dialogVisible.value = true
}

function openEdit(row: AdminSystem): void {
  editingId.value = row.id
  resetForm({
    code: row.code,
    name: row.name,
    description: row.description ?? '',
    nameTemplate: row.nameTemplate,
    sortOrder: row.sortOrder,
    attributes: row.attributes,
  })
  dialogVisible.value = true
}

function addAttribute(): void {
  form.attributes.push({ key: '', label: '', formula: '' })
}

function removeAttribute(index: number): void {
  form.attributes.splice(index, 1)
}

async function save(): Promise<void> {
  if (!form.code.trim() || !form.name.trim()) {
    ElMessage.warning('code 和名称不能为空')
    return
  }

  saving.value = true
  try {
    const payload = {
      code: form.code.trim(),
      name: form.name.trim(),
      description: form.description.trim() || null,
      nameTemplate: form.nameTemplate.trim(),
      sortOrder: Number(form.sortOrder) || 0,
      attributes: form.attributes
        .filter((item) => item.key.trim() && item.label.trim() && item.formula.trim())
        .map((item) => ({
          key: item.key.trim(),
          label: item.label.trim(),
          formula: item.formula.trim(),
        })),
    }

    if (editingId.value) {
      await http.put(`/api/admin/systems/${editingId.value}`, payload)
    } else {
      await http.post('/api/admin/systems', payload)
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

async function remove(row: AdminSystem): Promise<void> {
  try {
    await ElMessageBox.confirm(
      `删除规则系统「${row.name}」？它下面的素材会一起删除，有卡引用时会被拒绝。`,
      '确认删除',
      { type: 'warning' },
    )
  } catch {
    return
  }

  try {
    await http.delete(`/api/admin/systems/${row.id}`)
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
      <h2>规则系统</h2>
      <el-button size="small" @click="load">刷新</el-button>
      <span class="grow" />
      <el-button type="primary" @click="openCreate">新建规则系统</el-button>
    </div>

    <el-alert
      type="info"
      :closable="false"
      style="margin-bottom: 16px"
      title="属性模板里的 formula 支持 d20 / 4d6kh3 / 3d6*5 / 2d6*5+30；姓名模板用 {分组} 引用素材库里的姓名片段。"
    />

    <el-table :data="systems" border size="small">
      <el-table-column prop="code" label="code" width="120" />
      <el-table-column prop="name" label="名称" width="180" />
      <el-table-column label="属性模板" min-width="260">
        <template #default="{ row }">
          <el-tag v-for="attribute in row.attributes" :key="attribute.key" size="small" style="margin: 2px 4px 2px 0">
            {{ attribute.label }} {{ attribute.formula }}
          </el-tag>
          <span v-if="!row.attributes.length" class="muted">未配置</span>
        </template>
      </el-table-column>
      <el-table-column prop="nameTemplate" label="姓名模板" width="170" />
      <el-table-column prop="sortOrder" label="排序" width="70" />
      <el-table-column label="内置" width="70">
        <template #default="{ row }">
          <el-tag :type="row.isBuiltin ? 'warning' : 'info'" size="small">
            {{ row.isBuiltin ? '内置' : '自建' }}
          </el-tag>
        </template>
      </el-table-column>
      <el-table-column label="操作" width="140" fixed="right">
        <template #default="{ row }">
          <el-button link type="primary" @click="openEdit(row)">编辑</el-button>
          <el-button link type="danger" @click="remove(row)">删除</el-button>
        </template>
      </el-table-column>
    </el-table>

    <el-dialog v-model="dialogVisible" :title="editingId ? '编辑规则系统' : '新建规则系统'" width="760px">
      <el-form label-position="top">
        <div class="form-grid">
          <el-form-item label="code（小写，2-32 位）">
            <el-input v-model="form.code" placeholder="coc7" />
          </el-form-item>
          <el-form-item label="名称">
            <el-input v-model="form.name" placeholder="克苏鲁的呼唤 7版" />
          </el-form-item>
          <el-form-item label="姓名模板">
            <el-input v-model="form.nameTemplate" placeholder="{surname}{given}" />
          </el-form-item>
          <el-form-item label="排序">
            <el-input-number v-model="form.sortOrder" :min="0" :step="10" />
          </el-form-item>
        </div>

        <el-form-item label="描述">
          <el-input v-model="form.description" type="textarea" :rows="2" />
        </el-form-item>

        <el-form-item label="属性模板">
          <el-table :data="form.attributes" size="small" border>
            <el-table-column label="key" width="120">
              <template #default="{ row }"><el-input v-model="row.key" size="small" /></template>
            </el-table-column>
            <el-table-column label="名称" width="140">
              <template #default="{ row }"><el-input v-model="row.label" size="small" /></template>
            </el-table-column>
            <el-table-column label="公式">
              <template #default="{ row }"><el-input v-model="row.formula" size="small" placeholder="4d6kh3" /></template>
            </el-table-column>
            <el-table-column width="80">
              <template #default="{ $index }">
                <el-button link type="danger" @click="removeAttribute($index)">删除</el-button>
              </template>
            </el-table-column>
          </el-table>
          <el-button style="margin-top: 8px" size="small" @click="addAttribute">添加属性</el-button>
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="save">保存</el-button>
      </template>
    </el-dialog>
  </div>
</template>
