<template>
    <PageLayout>
        <div class="flex flex-col gap-4 p-4 md:flex-row">
            <IntegrationSettings />
            <LanguageSettings />
        </div>
    </PageLayout>
</template>

<script setup lang="ts">
import PageLayout from '@/components/layout/PageLayout.vue'
import LanguageSettings from '@/components/features/settings/LanguageSettings.vue'
import IntegrationSettings from '@/components/features/settings/IntegrationSettings.vue'
</script>
