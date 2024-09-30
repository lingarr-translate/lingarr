<template>
    <div class="flex flex-grow flex-col items-center justify-center p-4">
        <MediaEmojiIcon />
        <h2 class="mb-2 text-xl font-semibold">No Media Found</h2>
        <p class="mb-4 max-w-md text-center text-sm">
            It looks like your media hasn't been collected yet. Don't worry, we can fix that!
        </p>
        <div class="text-sm">
            <p class="font-medium">Next steps:</p>
            <ol class="mt-2 list-inside list-decimal text-left">
                <li>Check your integration settings</li>
                <li>Ensure all connections are properly configured</li>
                <li>Wait a few minutes for the application to collect your media</li>
            </ol>
        </div>
        <button
            class="mt-6 rounded-md border border-accent px-4 py-2 text-primary-content transition-colors hover:text-primary-content/50"
            @click="router.push({ name: 'integration-settings' })">
            Go to Settings
        </button>
    </div>
</template>
<script setup lang="ts">
import MediaEmojiIcon from '@/components/icons/MediaEmojiIcon.vue'
import { useRouter } from 'vue-router'

const router = useRouter()
</script>
