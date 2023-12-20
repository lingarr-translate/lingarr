import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router'

const routes = [
    {
        path: '/:path(.*)',
        component: () => import('@/pages/index.vue'),
        name: 'home',
        props: true
    }
] as RouteRecordRaw[]

const router = createRouter({
    history: createWebHistory(),
    routes
})

export default router
