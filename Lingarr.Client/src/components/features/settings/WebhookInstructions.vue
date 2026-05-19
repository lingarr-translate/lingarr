<template>
    <CardComponent title="Webhook">
        <template #description>
            Configure webhooks in Radarr and Sonarr to automatically trigger translations when new
            media is added.
        </template>
        <template #content>
            <div class="flex flex-col space-y-2">
                <span>
                    Go to
                    <b>Settings → Connect → Connections → +</b>
                    and use this URL:
                </span>
                <span class="font-semibold">Radarr</span>
                <CodeSnippet class="mt-1 block overflow-x-auto">{{ webhookUrl }}/api/webhook/radarr</CodeSnippet>
                <span class="font-semibold">Sonarr</span>
                <CodeSnippet class="mt-1 block overflow-x-auto">{{ webhookUrl }}/api/webhook/sonarr</CodeSnippet>
            </div>
        </template>
    </CardComponent>
</template>

<script setup lang="ts">
import CardComponent from '@/components/common/CardComponent.vue'
import CodeSnippet from '@/components/common/CodeSnippet.vue'

const webhookUrl = window.location.origin
</script>
