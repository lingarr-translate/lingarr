<template>
    <PageLayout>
        <div class="flex w-full items-end justify-end">
            <RoadmapComponent class="m-4 flex w-full flex-col md:max-w-md" />
        </div>
    </PageLayout>
</template>

<script setup lang="ts">
import PageLayout from '@/components/PageLayout.vue'
import RoadmapComponent from '@/components/RoadmapComponent.vue'
</script>
