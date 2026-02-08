<template>
    <div
        class="grid grid-flow-row auto-rows-max grid-cols-1 md:grid-cols-2 xl:grid-cols-2 2xl:grid-cols-3 gap-4 p-4">
        <AuthenticationSettings  />
        <ApiKeyConfiguration />
        <UserManagement class="col-span-1 md:col-span-2" />
    </div>
</template>
<script setup lang="ts">
import AuthenticationSettings from '@/components/features/settings/AuthenticationSettings.vue'
import UserManagement from '@/components/features/settings/UserManagement.vue'
import ApiKeyConfiguration from '@/components/features/settings/authentication/ApiKeyConfiguration.vue'
</script>
