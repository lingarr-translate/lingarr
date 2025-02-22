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
                name: 'services-settings',
                path: 'services',
                component: () => import('@/pages/settings/ServicesPage.vue')
            },
            {
                name: 'integration-settings',
                path: 'integration',
                component: () => import('@/pages/settings/IntegrationPage.vue')
            },
            {
                name: 'automation-settings',
                path: 'automation',
                component: () => import('@/pages/settings/AutomationPage.vue')
            },
            {
                name: 'mapping-settings',
                path: 'mapping',
                component: () => import('@/pages/settings/MappingPage.vue')
            },
            {
                name: 'tasks-settings',
                path: 'tasks',
                component: () => import('@/pages/settings/SchedulePage.vue')
            }
        ]
    }
] as RouteRecordRaw[]

const router = createRouter({
    history: createWebHistory(),
    routes
})

export default router
