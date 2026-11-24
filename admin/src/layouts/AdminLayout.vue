<script setup lang="ts">
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { clearSession, getStoredUser } from '../api/client'

const route = useRoute()
const router = useRouter()
const user = computed(() => getStoredUser())

const pageTitles: Record<string, string> = {
  overview: '概览',
  'creation-flow': '车卡流程',
}

const pageTitle = computed(() => pageTitles[String(route.name)] ?? '管理台')

function logout(): void {
  clearSession()
  router.push('/login')
}
</script>

<template>
  <el-container class="admin-shell">
    <el-aside width="236px" class="admin-aside">
      <div class="brand">
        <div class="brand-badge">T</div>
        <div class="brand-text">
          <div class="brand-title">TRPGHelper</div>
          <div class="brand-sub">内容管理台</div>
        </div>
      </div>

      <el-menu :default-active="route.path" router class="admin-menu">
        <el-menu-item index="/overview">概览</el-menu-item>
        <el-menu-item index="/creation-flow">车卡流程</el-menu-item>
      </el-menu>

      <div class="aside-foot">
        <span>v0.1 · 骨架版</span>
        <span class="aside-dot" />
      </div>
    </el-aside>

    <el-container class="admin-body">
      <el-header class="admin-header">
        <div class="header-title">
          <span class="header-crumb">管理台</span>
          <span class="header-sep">/</span>
          <span class="header-page">{{ pageTitle }}</span>
        </div>
        <div class="header-user">
          <el-tag v-if="user" type="success" effect="plain" round>{{ user.nickName }} · {{ user.role }}</el-tag>
          <el-button link type="primary" @click="logout">退出登录</el-button>
        </div>
      </el-header>

      <el-main class="admin-main">
        <router-view />
      </el-main>
    </el-container>
  </el-container>
</template>
