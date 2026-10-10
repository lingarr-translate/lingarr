<template>
    <div
        class="grid grid-flow-row auto-rows-max grid-cols-1 gap-6 p-4 min-[1800px]:grid-cols-3 sm:p-6 2xl:grid-cols-2">
        <AuthenticationSettings />
        <ApiKeyConfiguration />
        <UserManagement class="col-span-full" />
    </div>
</template>
<script setup lang="ts">
import AuthenticationSettings from '@/components/features/settings/AuthenticationSettings.vue'
import UserManagement from '@/components/features/settings/UserManagement.vue'
import ApiKeyConfiguration from '@/components/features/settings/authentication/ApiKeyConfiguration.vue'
</script>
