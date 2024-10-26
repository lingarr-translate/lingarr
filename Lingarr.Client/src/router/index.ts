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
        name: 'translations',
        path: '/translations',
        component: () => import('@/pages/TranslationPage.vue')
    },
    {
        path: '/settings',
        component: () => import('@/pages/SettingPage.vue'),
        name: 'settings',
        children: [
            {
                name: 'general-settings',
                path: 'general',
                component: () => import('@/pages/settings/GeneralPage.vue')
            },
            {
                name: 'integration-settings',
                path: 'integration',
                component: () => import('@/pages/settings/IntegrationPage.vue')
            },
            {
                name: 'mapping-settings',
                path: 'mapping',
                component: () => import('@/pages/settings/MappingPage.vue')
            }
        ]
    }
] as RouteRecordRaw[]

const router = createRouter({
    history: createWebHistory(),
    routes
})

export default router
