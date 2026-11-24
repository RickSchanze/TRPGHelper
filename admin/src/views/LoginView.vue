<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { http, errorMessage, setSession } from '../api/client'
import type { LoginResponse } from '../api/types'

const router = useRouter()
const nickName = ref('本地管理员')
const loading = ref(false)

async function devLogin(): Promise<void> {
  loading.value = true
  try {
    const { data } = await http.post<LoginResponse>('/api/auth/dev-login', { nickName: nickName.value })
    if (data.user.role !== 'admin') {
      ElMessage.error('这个账号不是管理员，检查后端的 Admin:DevLoginIsAdmin 或 openid 白名单。')
      return
    }

    setSession(data.token, data.user)
    ElMessage.success('登录成功')
    await router.push('/overview')
  } catch (error) {
    ElMessage.error(errorMessage(error))
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="login-page">
    <el-card class="login-card">
      <template #header>
        <div>
          <div class="login-brand">
            <div class="brand-badge">T</div>
            <h2 class="login-title">TRPGHelper 管理台</h2>
          </div>
          <p class="login-hint">
            本地开发登录（需要 ASPNETCORE_ENVIRONMENT=Development，且 Admin:DevLoginIsAdmin 为 true）
          </p>
        </div>
      </template>

      <el-form label-position="top" @submit.prevent>
        <el-form-item label="昵称">
          <el-input v-model="nickName" placeholder="本地管理员" @keyup.enter="devLogin" />
        </el-form-item>
        <el-button type="primary" style="width: 100%" :loading="loading" @click="devLogin">
          本地开发登录
        </el-button>
      </el-form>

      <el-alert
        style="margin-top: 16px"
        type="info"
        :closable="false"
        title="线上环境请用微信登录 + Admin:BootstrapOpenIds 白名单授予管理员角色。"
      />
    </el-card>
  </div>
</template>
