<template>
    <div class="relative">
        <select
            v-model="selectedLocale"
            class="rounded-md border border-accent bg-primary px-2 py-1">
            <option v-for="lang in languages" :key="lang.code" :value="lang.code">
                {{ lang.name }}
            </option>
        </select>
    </div>
</template>

<script setup lang="ts">
import { computed, WritableComputedRef } from 'vue'
import { useI18n } from '@/plugins/i18n'
import { ILocale } from '@/ts'
const { languages, locale, setLocale } = useI18n()

const selectedLocale: WritableComputedRef<ILocale> = computed({
    get: () => locale.value as ILocale,
    set: (value: ILocale) => setLocale(value)
})
</script>
