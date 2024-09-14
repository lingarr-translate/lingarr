import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
    {
        path: '/',
        component: () => import('@/pages/DashboardPage.vue'),
        name: 'dashboard'
    },
    {
        path: '/shows',
        component: () => import('@/pages/ShowPage.vue'),
        name: 'shows'
    },
    {
        path: '/movies',
        component: () => import('@/pages/MoviePage.vue'),
        name: 'movies'
    },
    {
        path: '/settings',
        component: () => import('@/pages/SettingPage.vue'),
        name: 'settings'
    }
] as RouteRecordRaw[]

const router = createRouter({
    history: createWebHistory(),
    routes
})

export default router
