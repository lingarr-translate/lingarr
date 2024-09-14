<template>
    <PageLayout>
        <div class="flex flex-col gap-4 p-4 md:flex-row">
            <IntegrationSettings />
            <LanguageSettings />
        </div>
    </PageLayout>
</template>

<script setup lang="ts">
import PageLayout from '@/components/PageLayout.vue'
import LanguageSettings from '@/components/LanguageSettings.vue'
import IntegrationSettings from '@/components/IntegrationSettings.vue'
</script>
