<template>
    <PageLayout>
        <div class="grid h-full grid-cols-[auto,1fr]">
            <aside class="w-16 shrink-0 bg-secondary md:w-40">
                <!-- Navigation -->
                <nav class="pt-4 md:pl-4 md:pt-8">
                    <ul class="flex flex-col space-y-4">
                        <li
                            v-for="(item, index) in menuItems"
                            :key="index"
                            class="w-full hover:brightness-150">
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

            <main class="flex p-4">
                <div
                    class="grid grid-flow-row auto-rows-max grid-cols-1 gap-4 xl:grid-cols-2 2xl:grid-cols-3">
                    <router-view></router-view>
                </div>
            </main>
        </div>
    </PageLayout>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import PageLayout from '@/components/layout/PageLayout.vue'
// import LanguageIcon from '@/components/icons/LanguageIcon.vue'
import IntegrationIcon from '@/components/icons/IntegrationIcon.vue'
import SettingIcon from '@/components/icons/SettingIcon.vue'

const router = useRouter()

const menuItems = [
    { label: 'General', icon: SettingIcon, route: 'general-settings' },
    { label: 'Integrations', icon: IntegrationIcon, route: 'integration-settings' }
    // { label: 'Translations', icon: LanguageIcon, route: 'translation-settings' }
]

function navigate(route: string) {
    router.push({ name: route })
}
</script>
