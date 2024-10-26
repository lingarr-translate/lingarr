<template>
    <PageLayout>
        <div
            class="grid grid-flow-row auto-rows-max grid-cols-1 gap-4 p-4 xl:grid-cols-2 2xl:grid-cols-3">
            <ChangeLog class="2xl:col-start-2" />
            <RoadmapComponent />
        </div>
    </PageLayout>
</template>

<script setup lang="ts">
import PageLayout from '@/components/layout/PageLayout.vue'
import RoadmapComponent from '@/components/features/dashboard/RoadmapComponent.vue'
import ChangeLog from '@/components/features/dashboard/ChangeLog.vue'
</script>
