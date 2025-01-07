<template>
    <div class="relative">
        <select
            v-model="selectedLocale"
            class="rounded-md border border-accent bg-primary px-2 py-1"
            @change="changeLanguage">
            <option v-for="lang in languages" :key="lang.code" :value="lang.code">
                {{ lang.name }}
            </option>
        </select>
    </div>
</template>

<script setup lang="ts">
import { computed, WritableComputedRef } from 'vue'
import { useI18n } from '@/plugins/i18n'
const { languages, locale, setLocale } = useI18n()

const selectedLocale: WritableComputedRef<string> = computed({
    get: () => locale.value,
    set: (value: string) => setLocale(value)
})

const changeLanguage = () => {
    setLocale(selectedLocale.value)
}
</script>
