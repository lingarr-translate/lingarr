import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router'
import { useInstanceStore } from '@/store/instance'

const routes: RouteRecordRaw[] = [
    // Auth
    {
        path: '/auth',
        component: () => import('@/components/layout/AuthLayout.vue'),
        children: [
            {
                path: '/auth/onboarding',
                component: () => import('@/pages/OnboardingPage.vue'),
                name: 'onboarding',
                meta: { authenticated: false }
            },
            {
                path: '/auth/login',
                component: () => import('@/pages/LoginPage.vue'),
                name: 'login',
                meta: { authenticated: false }
            }
        ]
    },

    // Main
    {
        path: '/',
        component: () => import('@/components/layout/MainLayout.vue'),
        meta: { authenticated: true },
        children: [
            {
                path: '',
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
                path: '/translations',
                component: () => import('@/pages/TranslationPage.vue'),
                name: 'translations'
            },
            {
                path: '/settings',
                component: () => import('@/pages/SettingPage.vue'),
                name: 'settings',
                children: [
                    {
                        path: 'services',
                        name: 'services-settings',
                        component: () => import('@/pages/settings/ServicesPage.vue')
                    },
                    {
                        path: 'integration',
                        name: 'integration-settings',
                        component: () => import('@/pages/settings/IntegrationPage.vue')
                    },
                    {
                        path: 'authentication',
                        name: 'authentication-settings',
                        component: () => import('@/pages/settings/AuthenticationPage.vue')
                    },
                    {
                        path: 'subtitle',
                        name: 'subtitle-settings',
                        component: () => import('@/pages/settings/SubtitlePage.vue')
                    },
                    {
                        path: 'automation',
                        name: 'automation-settings',
                        component: () => import('@/pages/settings/AutomationPage.vue')
                    },
                    {
                        path: 'mapping',
                        name: 'mapping-settings',
                        component: () => import('@/pages/settings/MappingPage.vue')
                    },
                    {
                        path: 'tasks',
                        name: 'tasks-settings',
                        component: () => import('@/pages/settings/SchedulePage.vue')
                    },
                    {
                        path: 'logs',
                        name: 'logs-settings',
                        component: () => import('@/pages/settings/LogsPage.vue')
                    }
                ]
            }
        ]
    }
]

const router = createRouter({
    history: createWebHistory(),
    routes
})

router.beforeEach(async (to, _, next) => {
    if (!to.meta.authenticated) {
        next()
        return
    }

    const instanceStore = useInstanceStore()
    const isAuthenticated = await instanceStore.ensureAuthenticated()

    if (!isAuthenticated && to.name !== 'login') {
        next({ name: 'login' })
        return
    }

    next()
})

export default router
