import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router'

const routes: RouteRecordRaw[] = [
    {
        path: '/',
        redirect: '/tvshows'
    },
    {
        path: '/tvshows',
        component: () => import('@/pages/IndexPage.vue'),
        name: 'tvshows'
    },
    {
        path: '/movies',
        component: () => import('@/pages/IndexPage.vue'),
        name: 'movies'
    },
    {
        path: '/settings',
        component: () => import('@/pages/SettingsPage.vue'),
        name: 'settings'
    }
] as RouteRecordRaw[]

const router = createRouter({
    history: createWebHistory(),
    routes
})

export default router
