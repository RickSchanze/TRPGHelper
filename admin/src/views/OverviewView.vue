<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import { errorMessage, http } from '../api/client'
import type { AdminOverview } from '../api/types'

const loading = ref(false)
const overview = ref<AdminOverview | null>(null)

const kindLabels: Record<string, string> = {
  namePart: '姓名片段',
  occupation: '职业',
  background: '背景',
  class: '职业卡',
  race: '种族',
  inspiration: '灵感',
}

async function load(): Promise<void> {
  loading.value = true
  try {
    const { data } = await http.get<AdminOverview>('/api/admin/overview')
    overview.value = data
  } catch (error) {
    ElMessage.error(errorMessage(error))
  } finally {
    loading.value = false
  }
}

onMounted(load)
</script>

<template>
  <div v-loading="loading">
    <div class="page-title">
      <h2>概览</h2>
      <el-button size="small" @click="load">刷新</el-button>
    </div>

    <el-row :gutter="16">
      <el-col :span="4">
        <el-card class="stat-card">
          <div class="value">{{ overview?.systems ?? '-' }}</div>
          <div class="label">规则系统</div>
        </el-card>
      </el-col>
      <el-col :span="4">
        <el-card class="stat-card">
          <div class="value">{{ overview?.generatorEntries ?? '-' }}</div>
          <div class="label">素材总数</div>
        </el-card>
      </el-col>
      <el-col :span="4">
        <el-card class="stat-card">
          <div class="value">{{ overview?.enabledGeneratorEntries ?? '-' }}</div>
          <div class="label">启用中素材</div>
        </el-card>
      </el-col>
      <el-col :span="4">
        <el-card class="stat-card">
          <div class="value">{{ overview?.users ?? '-' }}</div>
          <div class="label">用户</div>
        </el-card>
      </el-col>
      <el-col :span="4">
        <el-card class="stat-card">
          <div class="value">{{ overview?.admins ?? '-' }}</div>
          <div class="label">管理员</div>
        </el-card>
      </el-col>
      <el-col :span="4">
        <el-card class="stat-card">
          <div class="value">{{ overview?.cards ?? '-' }}</div>
          <div class="label">角色卡</div>
        </el-card>
      </el-col>
    </el-row>

    <el-card style="margin-top: 16px">
      <template #header>素材分布</template>
      <el-table :data="overview?.entriesByKind ?? []" size="small">
        <el-table-column label="类型">
          <template #default="{ row }">{{ kindLabels[row.kind] ?? row.kind }}</template>
        </el-table-column>
        <el-table-column prop="kind" label="kind 键" width="160" />
        <el-table-column prop="count" label="条数" width="120" />
      </el-table>
      <p class="muted" style="margin: 12px 0 0">
        数据来自 <code>/api/admin/overview</code>，规则系统、素材的增删改都会即时反映在这里。
      </p>
    </el-card>
  </div>
</template>
