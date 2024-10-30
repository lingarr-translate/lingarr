<template>
    <PageLayout>
        <div class="grid h-full grid-cols-[auto,1fr]">
            <aside class="w-[3.175rem] shrink-0 bg-secondary md:w-40">
                <nav class="pt-4 md:pl-4 md:pt-8">
                    <ul class="flex flex-col space-y-4">
                        <li
                            v-for="(item, index) in menuItems"
                            :key="index"
                            class="w-full hover:brightness-150"
                            :class="[
                                'w-full hover:brightness-150',
                                { 'brightness-150': route.name === item.route }
                            ]">
                            <a
                                :title="item.label"
                                :aria-label="item.label"
                                class="flex w-full cursor-pointer items-center justify-center md:justify-start"
                                @click="navigate(item.route)">
                                <component :is="item.icon" class="h-4 w-4 md:mr-3" />
                                <span class="hidden text-sm md:inline-block">
                                    {{ item.label }}
                                </span>
                            </a>
                        </li>
                    </ul>
                </nav>
            </aside>

            <main class="flex">
                <router-view></router-view>
            </main>
        </div>
    </PageLayout>
</template>

<script setup lang="ts">
import { useRoute, useRouter } from 'vue-router'
import PageLayout from '@/components/layout/PageLayout.vue'
import IntegrationIcon from '@/components/icons/IntegrationIcon.vue'
import SettingIcon from '@/components/icons/SettingIcon.vue'
import FoldersIcon from '@/components/icons/FoldersIcon.vue'
import AutomationIcon from '@/components/icons/AutomationIcon.vue'

const router = useRouter()
const route = useRoute()

const menuItems = [
    { label: 'Integrations', icon: IntegrationIcon, route: 'integration-settings' },
    { label: 'Services', icon: SettingIcon, route: 'services-settings' },
    { label: 'Automation', icon: AutomationIcon, route: 'automation-settings' },
    { label: 'Mapping', icon: FoldersIcon, route: 'mapping-settings' }
]

function navigate(route: string) {
    router.push({ name: route })
}
</script>
