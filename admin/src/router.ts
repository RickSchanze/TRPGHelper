import { createRouter, createWebHashHistory } from 'vue-router'
import { getToken } from './api/client'
import AdminLayout from './layouts/AdminLayout.vue'
import LoginView from './views/LoginView.vue'
import OverviewView from './views/OverviewView.vue'
import CreationFlowView from './views/CreationFlowView.vue'

export const router = createRouter({
  history: createWebHashHistory(),
  routes: [
    { path: '/login', name: 'login', component: LoginView },
    {
      path: '/',
      component: AdminLayout,
      redirect: '/overview',
      children: [
        { path: 'overview', name: 'overview', component: OverviewView },
        { path: 'creation-flow', name: 'creation-flow', component: CreationFlowView },
      ],
    },
    { path: '/:pathMatch(.*)*', redirect: '/overview' },
  ],
})

router.beforeEach((to) => {
  const token = getToken()
  if (to.path !== '/login' && !token) {
    return { path: '/login' }
  }
  if (to.path === '/login' && token) {
    return { path: '/overview' }
  }
  return true
})
